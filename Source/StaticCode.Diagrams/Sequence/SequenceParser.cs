using System.Globalization;
using System.Text.RegularExpressions;
using StaticCode.Diagrams.Text;

namespace StaticCode.Diagrams.Sequence;

/// <summary>
/// Parses the sequence diagram subset of Mermaid syntax: participants and actors, messages with all basic arrow
/// types, activations, notes, autonumbering, and the <c>loop</c>, <c>alt</c>, <c>opt</c>, <c>par</c>,
/// <c>critical</c>, <c>break</c>, and <c>rect</c> blocks.
/// </summary>
public sealed class SequenceParser
{
    private const string Header = "sequenceDiagram";
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Regex ParticipantRegex = new(@"^(?<kind>participant|actor)\s+(?<id>.+?)(?:\s+as\s+(?<label>.+))?$", Options);
    private static readonly Regex MessageRegex = new(
        @"^(?<from>[^-<>:+;,]+?)\s*(?<arrow><<-->>|<<->>|-->>|->>|--x|-x|--\)|-\)|-->|->)\s*(?<modifier>[+-])?\s*(?<to>[^-<>:+;,]+?)\s*(?::(?<text>.*))?$",
        Options);
    private static readonly Regex NoteRegex = new(
        @"^note\s+(?<placement>right\s+of|left\s+of|over)\s+(?<participants>[^:]+?)\s*:(?<text>.*)$",
        Options | RegexOptions.IgnoreCase);
    private static readonly Regex BlockStartRegex = new(@"^(?<kind>loop|alt|opt|par|critical|break|rect)(?:\s+(?<label>.*))?$", Options);
    private static readonly Regex BlockSectionRegex = new(@"^(?<kind>else|and|option)(?:\s+(?<label>.*))?$", Options);
    private static readonly Regex ActivationRegex = new(@"^(?<kind>activate|deactivate)\s+(?<id>.+)$", Options);
    private static readonly Regex AutonumberRegex = new(@"^autonumber(?:\s+(?<off>off)|\s+(?<start>\d+)(?:\s+(?<step>\d+))?)?$", Options);
    private static readonly Regex TitleRegex = new(@"^title(?:\s*:\s*|\s+)(?<title>.+)$", Options);
    private static readonly Regex AccessibleTitleRegex = new(@"^accTitle\s*:\s*(?<title>.+)$", Options);
    private static readonly Regex AccessibleDescriptionRegex = new(@"^accDescr\s*:", Options);
    private static readonly Regex UnsupportedRegex = new(@"^(?<keyword>box|create|destroy|links?|properties|details|accDescr)\b", Options);
    private static readonly Regex IdRegex = new(@"^[^-<>:+;,@{}\[\]()""]+$", Options);

    private readonly string[] _lines;
    private readonly Dictionary<string, SequenceParticipant> _participants = new(StringComparer.Ordinal);
    private readonly List<SequenceParticipant> _participantOrder = new();
    private readonly Dictionary<string, int> _activations = new(StringComparer.Ordinal);
    private readonly List<SequenceEvent> _events = new();
    private readonly Stack<BlockKind> _blocks = new();
    private string? _title;
    private string? _accessibleTitle;
    private int _lineNumber;

    private SequenceParser(string source)
    {
        _lines = source.ReplaceLineEndings("\n").Split('\n');
    }

    /// <summary>Returns <c>true</c> when the first statement is a <c>sequenceDiagram</c> header.</summary>
    public static bool CanParse(string source) =>
        source.ReplaceLineEndings("\n").Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !line.StartsWith("%%", StringComparison.Ordinal)) == Header;

    public static SequenceDiagram Parse(string source) => new SequenceParser(source).Parse();

    private SequenceDiagram Parse()
    {
        var headerFound = false;
        for (var index = 0; index < _lines.Length; index++)
        {
            var line = _lines[index].Trim();
            if (line.EndsWith(';')) line = line[..^1].TrimEnd();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal)) continue;
            _lineNumber = index + 1;

            if (!headerFound)
            {
                if (line != Header) throw Error("Expected a 'sequenceDiagram' header.");
                headerFound = true;
                continue;
            }

            ParseStatement(line);
        }

        if (!headerFound) throw new DiagramSyntaxException("Missing 'sequenceDiagram' header.", 1);
        if (_blocks.Count > 0) throw Error($"Missing 'end' for '{_blocks.Peek().ToString().ToLowerInvariant()}'.");
        if (_participantOrder.Count == 0) throw Error("A sequence diagram needs at least one participant.");
        return new SequenceDiagram(_title, _accessibleTitle, _participantOrder, _events);
    }

    private void ParseStatement(string line)
    {
        if (line == "end")
        {
            if (_blocks.Count == 0) throw Error("'end' without an open block.");
            _blocks.Pop();
            _events.Add(new SequenceBlockEnd());
            return;
        }

        if (TitleRegex.Match(line) is { Success: true } title)
        {
            _title = MermaidText.Clean(title.Groups["title"].Value);
            return;
        }

        if (AccessibleTitleRegex.Match(line) is { Success: true } accessibleTitle)
        {
            _accessibleTitle = accessibleTitle.Groups["title"].Value.Trim();
            return;
        }

        if (AccessibleDescriptionRegex.IsMatch(line)) return;

        if (UnsupportedRegex.Match(line) is { Success: true } unsupported)
            throw Error($"'{unsupported.Groups["keyword"].Value}' is not supported yet.");

        if (AutonumberRegex.Match(line) is { Success: true } autonumber)
        {
            _events.Add(new SequenceAutonumber(
                !autonumber.Groups["off"].Success,
                autonumber.Groups["start"].Success ? ParseNumber(autonumber.Groups["start"].Value) : 1,
                autonumber.Groups["step"].Success ? ParseNumber(autonumber.Groups["step"].Value) : 1));
            return;
        }

        if (ParticipantRegex.Match(line) is { Success: true } participant)
        {
            var declared = GetParticipant(participant.Groups["id"].Value);
            declared.Kind = participant.Groups["kind"].Value == "actor" ? ParticipantKind.Actor : ParticipantKind.Participant;
            if (participant.Groups["label"].Success) declared.Label = MermaidText.Clean(participant.Groups["label"].Value);
            return;
        }

        if (ActivationRegex.Match(line) is { Success: true } activation)
        {
            var id = GetParticipant(activation.Groups["id"].Value).Id;
            var active = activation.Groups["kind"].Value == "activate";
            ChangeActivation(id, active);
            _events.Add(new SequenceActivation(id, active));
            return;
        }

        if (NoteRegex.Match(line) is { Success: true } note)
        {
            _events.Add(ParseNote(note));
            return;
        }

        if (BlockStartRegex.Match(line) is { Success: true } block)
        {
            var kind = Enum.Parse<BlockKind>(block.Groups["kind"].Value, ignoreCase: true);
            // A rect's argument is a color, which the theme replaces.
            var label = kind == BlockKind.Rect ? null : CleanOrNull(block.Groups["label"].Value);
            _blocks.Push(kind);
            _events.Add(new SequenceBlockStart(kind, label));
            return;
        }

        if (BlockSectionRegex.Match(line) is { Success: true } section)
        {
            var keyword = section.Groups["kind"].Value;
            var expected = keyword switch { "else" => BlockKind.Alt, "and" => BlockKind.Par, _ => BlockKind.Critical };
            if (_blocks.Count == 0 || _blocks.Peek() != expected)
                throw Error($"'{keyword}' is only allowed inside '{expected.ToString().ToLowerInvariant()}'.");
            _events.Add(new SequenceBlockSection(CleanOrNull(section.Groups["label"].Value)));
            return;
        }

        if (MessageRegex.Match(line) is { Success: true } message)
        {
            _events.Add(ParseMessage(message));
            return;
        }

        throw Error($"Unsupported statement '{line}'.");
    }

    private SequenceMessage ParseMessage(Match match)
    {
        var from = GetParticipant(match.Groups["from"].Value).Id;
        var to = GetParticipant(match.Groups["to"].Value).Id;
        var arrow = match.Groups["arrow"].Value;
        var modifier = match.Groups["modifier"].Value;

        var bidirectional = arrow.StartsWith("<<", StringComparison.Ordinal);
        var line = arrow.Contains("--", StringComparison.Ordinal) ? MessageLine.Dotted : MessageLine.Solid;
        var head = arrow[^1] switch
        {
            '>' when arrow.EndsWith(">>", StringComparison.Ordinal) => MessageHead.Arrow,
            'x' => MessageHead.Cross,
            ')' => MessageHead.Open,
            _ => MessageHead.None
        };

        if (modifier == "+") ChangeActivation(to, true);
        if (modifier == "-") ChangeActivation(from, false);

        return new SequenceMessage(
            from,
            to,
            CleanOrNull(match.Groups["text"].Value),
            line,
            head,
            bidirectional,
            ActivateTarget: modifier == "+",
            DeactivateSource: modifier == "-");
    }

    private SequenceNote ParseNote(Match match)
    {
        var placement = match.Groups["placement"].Value.ToLowerInvariant() switch
        {
            var value when value.StartsWith("right", StringComparison.Ordinal) => NotePlacement.RightOf,
            var value when value.StartsWith("left", StringComparison.Ordinal) => NotePlacement.LeftOf,
            _ => NotePlacement.Over
        };
        var ids = match.Groups["participants"].Value.Split(',');
        if (ids.Length > 2 || (ids.Length == 2 && placement != NotePlacement.Over))
            throw Error("Only 'Note over' can span two participants.");

        var first = GetParticipant(ids[0]).Id;
        var second = ids.Length == 2 ? GetParticipant(ids[1]).Id : null;
        if (second == first) second = null;
        return new SequenceNote(placement, first, second, MermaidText.Clean(match.Groups["text"].Value));
    }

    private void ChangeActivation(string id, bool active)
    {
        var depth = _activations.GetValueOrDefault(id);
        if (!active && depth == 0) throw Error($"'{id}' is not active.");
        _activations[id] = depth + (active ? 1 : -1);
    }

    private SequenceParticipant GetParticipant(string rawId)
    {
        var id = rawId.Trim();
        if (!IdRegex.IsMatch(id)) throw Error($"Invalid participant '{id}'.");
        if (_participants.TryGetValue(id, out var participant)) return participant;

        participant = new SequenceParticipant(id);
        _participants.Add(id, participant);
        _participantOrder.Add(participant);
        return participant;
    }

    private static string? CleanOrNull(string text)
    {
        var cleaned = MermaidText.Clean(text);
        return cleaned.Length == 0 ? null : cleaned;
    }

    private int ParseNumber(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : throw Error($"Invalid number '{value}'.");

    private DiagramSyntaxException Error(string message) => new(message, _lineNumber);
}
