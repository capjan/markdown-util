using System.Globalization;
using System.Text.RegularExpressions;

namespace StaticCode.Diagrams.Flowchart;

/// <summary>
/// Parses the flowchart subset of Mermaid syntax: nodes with basic shapes, links with labels,
/// link chains, and <c>&amp;</c> groups. Styling statements are ignored because colors come from the page theme.
/// </summary>
public sealed class FlowchartParser
{
    private static readonly Regex HeaderRegex = new(
        @"^(?:flowchart|graph)(?:[ \t]+(?<direction>TB|TD|BT|LR|RL))?[ \t]*(?:;(?<rest>.*))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex KeywordRegex = new(@"\G(?<keyword>[A-Za-z]+)(?=[ \t:{]|$)", RegexOptions.Compiled);
    private static readonly Regex IdRegex = new(@"\G[\p{L}\p{N}_]+", RegexOptions.Compiled);
    private static readonly Regex ClassSuffixRegex = new(@"\G:::[\w-]+", RegexOptions.Compiled);
    private static readonly Regex TextLinkOpenRegex = new(@"\G(?<start>[<ox])?(?<kind>--|==|-\.)(?=[ \t])", RegexOptions.Compiled);
    private static readonly Regex SolidCloseRegex = new(@"(?<line>-{3,}|--(?=[>ox]))(?<end>[>ox])?", RegexOptions.Compiled);
    private static readonly Regex ThickCloseRegex = new(@"(?<line>={3,}|==(?=[>ox]))(?<end>[>ox])?", RegexOptions.Compiled);
    private static readonly Regex DottedCloseRegex = new(@"(?<line>\.+-)(?<end>[>ox])?", RegexOptions.Compiled);
    private static readonly Regex SimpleLinkRegex = new(
        @"\G(?<start>[<ox])?(?:(?<solid>-{3,}|--(?=[>ox]))|(?<thick>={3,}|==(?=[>ox]))|-(?<dots>\.+)-)(?<end>[>ox])?",
        RegexOptions.Compiled);
    private static readonly Regex LineBreakRegex = new(@"<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EntityRegex = new(@"#(?<code>\d+|[a-zA-Z]+);", RegexOptions.Compiled);

    private static readonly (string Open, string Close, NodeShape Shape)[] Shapes =
    {
        ("((", "))", NodeShape.Circle),
        ("([", "])", NodeShape.Stadium),
        ("(", ")", NodeShape.Rounded),
        ("[", "]", NodeShape.Rectangle),
        ("{", "}", NodeShape.Diamond)
    };

    private static readonly string[] UnsupportedShapeOpeners = { "(((", "[[", "[(", "[/", "[\\", "{{", ">", "@{" };
    private static readonly string[] IgnoredKeywords = { "classDef", "class", "style", "linkStyle", "click", "accDescr" };
    private static readonly string[] UnsupportedKeywords = { "subgraph", "end", "direction" };

    private readonly string[] _lines;
    private readonly Dictionary<string, FlowchartNode> _nodes = new(StringComparer.Ordinal);
    private readonly List<FlowchartNode> _nodeOrder = new();
    private readonly List<FlowchartEdge> _edges = new();
    private string? _title;
    private string _text = string.Empty;
    private int _pos;
    private int _lineNumber;

    private FlowchartParser(string source)
    {
        _lines = source.ReplaceLineEndings("\n").Split('\n');
    }

    /// <summary>Returns <c>true</c> when the first statement is a <c>flowchart</c> or <c>graph</c> header.</summary>
    public static bool CanParse(string source)
    {
        var firstLine = source.ReplaceLineEndings("\n").Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !line.StartsWith("%%", StringComparison.Ordinal));
        return firstLine != null && HeaderRegex.IsMatch(firstLine);
    }

    public static FlowchartDiagram Parse(string source) => new FlowchartParser(source).Parse();

    private FlowchartDiagram Parse()
    {
        FlowDirection? direction = null;
        for (var index = 0; index < _lines.Length; index++)
        {
            var line = _lines[index].Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal)) continue;
            _lineNumber = index + 1;

            if (direction == null)
            {
                var header = HeaderRegex.Match(line);
                if (!header.Success) throw Error("Expected a 'flowchart' or 'graph' header.");
                direction = ToDirection(header.Groups["direction"].Value);
                if (header.Groups["rest"].Success) ParseLine(header.Groups["rest"].Value);
                continue;
            }

            ParseLine(line);
        }

        if (direction == null) throw new DiagramSyntaxException("Missing 'flowchart' header.", 1);
        return new FlowchartDiagram(direction.Value, _nodeOrder, _edges, _title);
    }

    private static FlowDirection ToDirection(string value) => value switch
    {
        "BT" => FlowDirection.BottomToTop,
        "LR" => FlowDirection.LeftToRight,
        "RL" => FlowDirection.RightToLeft,
        _ => FlowDirection.TopToBottom
    };

    private void ParseLine(string line)
    {
        _text = line;
        _pos = 0;
        while (true)
        {
            SkipSpaces();
            if (AtEnd) return;
            if (Peek == ';')
            {
                _pos++;
                continue;
            }

            if (TryHandleKeyword()) return;

            ParseStatement();
            SkipSpaces();
            if (!AtEnd && Peek != ';') throw Error($"Unexpected '{_text[_pos..]}'.");
        }
    }

    private bool TryHandleKeyword()
    {
        var match = KeywordRegex.Match(_text, _pos);
        if (!match.Success) return false;

        var keyword = match.Groups["keyword"].Value;
        if (UnsupportedKeywords.Contains(keyword)) throw Error($"'{keyword}' is not supported yet.");
        if (IgnoredKeywords.Contains(keyword)) return true;
        if (keyword != "accTitle") return false;

        var rest = _text[(_pos + match.Length)..].TrimStart();
        if (!rest.StartsWith(':')) throw Error("Expected ':' after 'accTitle'.");
        _title = rest[1..].Trim();
        return true;
    }

    private void ParseStatement()
    {
        var sources = ParseNodeGroup();
        while (true)
        {
            SkipSpaces();
            if (AtEnd || Peek == ';') return;

            var link = ParseLink();
            SkipSpaces();
            var targets = ParseNodeGroup();
            foreach (var source in sources)
            foreach (var target in targets)
                _edges.Add(link with { From = source.Id, To = target.Id });
            sources = targets;
        }
    }

    private List<FlowchartNode> ParseNodeGroup()
    {
        var nodes = new List<FlowchartNode> { ParseNode() };
        while (true)
        {
            var position = _pos;
            SkipSpaces();
            if (AtEnd || Peek != '&')
            {
                _pos = position;
                return nodes;
            }

            _pos++;
            SkipSpaces();
            nodes.Add(ParseNode());
        }
    }

    private FlowchartNode ParseNode()
    {
        var match = IdRegex.Match(_text, _pos);
        if (!match.Success) throw Error(AtEnd ? "Expected a node." : $"Expected a node at '{_text[_pos..]}'.");
        _pos += match.Length;

        if (!_nodes.TryGetValue(match.Value, out var node))
        {
            node = new FlowchartNode(match.Value);
            _nodes.Add(node.Id, node);
            _nodeOrder.Add(node);
        }

        var unsupported = UnsupportedShapeOpeners.FirstOrDefault(opener => Rest.StartsWith(opener, StringComparison.Ordinal));
        if (unsupported != null) throw Error($"Node shape '{unsupported}' is not supported yet.");

        foreach (var (open, close, shape) in Shapes)
        {
            if (!Rest.StartsWith(open, StringComparison.Ordinal)) continue;
            _pos += open.Length;
            node.Label = ReadText(close);
            node.Shape = shape;
            break;
        }

        var classSuffix = ClassSuffixRegex.Match(_text, _pos);
        if (classSuffix.Success) _pos += classSuffix.Length;
        return node;
    }

    private FlowchartEdge ParseLink()
    {
        FlowchartEdge edge;
        var textLink = TextLinkOpenRegex.Match(_text, _pos);
        if (textLink.Success)
        {
            var kind = textLink.Groups["kind"].Value;
            var closeRegex = kind switch
            {
                "--" => SolidCloseRegex,
                "==" => ThickCloseRegex,
                _ => DottedCloseRegex
            };
            var textStart = textLink.Index + textLink.Length;
            var close = closeRegex.Match(_text, textStart);
            if (!close.Success) throw Error($"Missing the end of the link that starts with '{kind}'.");

            var line = close.Groups["line"].Value;
            var hasEnd = close.Groups["end"].Success;
            edge = new FlowchartEdge(
                string.Empty,
                string.Empty,
                CleanLabel(_text[textStart..close.Index]),
                kind switch { "--" => LineStyle.Solid, "==" => LineStyle.Thick, _ => LineStyle.Dotted },
                ToMarker(textLink.Groups["start"].Value),
                ToMarker(close.Groups["end"].Value),
                kind == "-." ? line.Length - 1 : LineLength(line.Length, hasEnd));
            _pos = close.Index + close.Length;
        }
        else
        {
            var link = SimpleLinkRegex.Match(_text, _pos);
            if (!link.Success) throw Error($"Expected a link such as '-->' at '{_text[_pos..]}'.");

            var hasEnd = link.Groups["end"].Success;
            var (style, length) =
                link.Groups["solid"].Success ? (LineStyle.Solid, LineLength(link.Groups["solid"].Length, hasEnd)) :
                link.Groups["thick"].Success ? (LineStyle.Thick, LineLength(link.Groups["thick"].Length, hasEnd)) :
                (LineStyle.Dotted, link.Groups["dots"].Length);
            edge = new FlowchartEdge(
                string.Empty,
                string.Empty,
                null,
                style,
                ToMarker(link.Groups["start"].Value),
                ToMarker(link.Groups["end"].Value),
                length);
            _pos += link.Length;
        }

        var position = _pos;
        SkipSpaces();
        if (!AtEnd && Peek == '|')
        {
            var end = _text.IndexOf('|', _pos + 1);
            if (end < 0) throw Error("Missing closing '|' of the link label.");
            edge = edge with { Label = CleanLabel(_text[(_pos + 1)..end]) };
            _pos = end + 1;
        }
        else
        {
            _pos = position;
        }

        return edge;
    }

    private static int LineLength(int characters, bool hasEnd) => Math.Max(1, characters - (hasEnd ? 1 : 2));

    private static EdgeMarker ToMarker(string value) => value switch
    {
        "<" or ">" => EdgeMarker.Arrow,
        "o" => EdgeMarker.Circle,
        "x" => EdgeMarker.Cross,
        _ => EdgeMarker.None
    };

    private string ReadText(string close)
    {
        SkipSpaces();
        string text;
        if (!AtEnd && Peek == '"')
        {
            var end = _text.IndexOf('"', _pos + 1);
            if (end < 0) throw Error("Missing closing '\"'.");
            text = _text[(_pos + 1)..end];
            _pos = end + 1;
            SkipSpaces();
            if (!Rest.StartsWith(close, StringComparison.Ordinal)) throw Error($"Expected '{close}'.");
        }
        else
        {
            var end = _text.IndexOf(close, _pos, StringComparison.Ordinal);
            if (end < 0) throw Error($"Missing '{close}'.");
            text = _text[_pos..end];
            _pos = end;
        }

        _pos += close.Length;
        return CleanLabel(text) ?? string.Empty;
    }

    private static string? CleanLabel(string text)
    {
        text = text.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"') text = text[1..^1];
        text = LineBreakRegex.Replace(text, "\n");
        text = EntityRegex.Replace(text, match => DecodeEntity(match.Groups["code"].Value) ?? match.Value);
        var lines = text.Split('\n').Select(line => line.Trim());
        text = string.Join('\n', lines).Trim();
        return text.Length == 0 ? null : text;
    }

    private static string? DecodeEntity(string code)
    {
        if (int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var codePoint))
            return codePoint is > 0 and <= 0x10FFFF ? char.ConvertFromUtf32(codePoint) : null;

        return code switch
        {
            "quot" => "\"",
            "amp" => "&",
            "lt" => "<",
            "gt" => ">",
            "nbsp" => " ",
            _ => null
        };
    }

    private bool AtEnd => _pos >= _text.Length;
    private char Peek => _text[_pos];
    private ReadOnlySpan<char> Rest => _text.AsSpan(_pos);

    private void SkipSpaces()
    {
        while (!AtEnd && Peek is ' ' or '\t') _pos++;
    }

    private DiagramSyntaxException Error(string message) => new(message, _lineNumber);
}
