using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Markdown.Renderer;

public sealed record PdfSourceSection(string Title, string RelativeHtmlPath, string Markdown, int Depth = 0);

/// <summary>Writes a paginated PDF from the HTML fragment produced by MDU's Markdown pipeline.</summary>
public sealed class PdfRenderer(HtmlRenderer htmlRenderer)
{
    public void WritePdf(Stream output, string title, IReadOnlyList<PdfSourceSection> sections, bool includeFrontMatter = true)
    {
        var layout = new PdfLayout(htmlRenderer);
        foreach (var section in sections)
            layout.RenderSection(section);

        var result = layout.Complete();
        var coverTitle = sections.FirstOrDefault()?.Title ?? title;
        var description = sections.Count == 0 ? string.Empty : GetDescription(sections[0].Markdown);
        var frontMatter = includeFrontMatter ? PdfFrontMatter.Create(coverTitle, description, sections, result) :
            new PdfFrontMatterResult([]);
        var pages = frontMatter.Pages.Concat(result.Pages).ToArray();
        var destinations = result.Destinations.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with { PageIndex = pair.Value.PageIndex + frontMatter.Pages.Count },
            StringComparer.Ordinal);
        PdfLayout.AddFooters(pages);
        PdfFileWriter.Write(output, coverTitle, pages, destinations);
    }

    private string GetDescription(string markdown)
    {
        var html = PdfLayout.NormalizeVoidElements(htmlRenderer.RenderMarkdownFragment(markdown));
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml($"<root>{html}</root>");
        return PdfLayout.CollapseWhitespace(document.SelectSingleNode("//p")?.InnerText ?? string.Empty);
    }
}

internal sealed class PdfLayout
{
    internal const double PageWidth = 595.28;
    internal const double PageHeight = 841.89;
    internal const double Left = 48;
    internal const double Right = 48;
    internal const double Top = 48;
    internal const double Bottom = 48;
    // Keep 17px HTML body text at the same physical size as 10.5pt PDF body text.
    private const double SvgCssPixelsToPdfPoints = 10.5 / 17.0;
    private readonly HtmlRenderer _htmlRenderer;
    private readonly List<PdfPageContent> _pages = [];
    private readonly Dictionary<string, PdfDestination> _destinations = new(StringComparer.Ordinal);
    private PdfPageContent _page;
    private double _y;
    private string _sectionPath = string.Empty;

    public PdfLayout(HtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer;
        _page = NewPage();
        _y = Top;
    }

    public PdfLayoutResult Complete()
    {
        return new PdfLayoutResult(_pages, _destinations);
    }

    internal static void AddFooters(IReadOnlyList<PdfPageContent> pages)
    {
        for (var index = 0; index < pages.Count; index++)
        {
            var page = pages[index];
            page.Content.Append("BT /F1 8 Tf 0.42 0.42 0.45 rg 1 0 0 1 ")
                .Append(F(PageWidth - Right - 15)).Append(' ').Append(F(20)).Append(" Tm <")
                .Append(PdfEncoding.Hex((index + 1).ToString(CultureInfo.InvariantCulture))).Append("> Tj ET\n");
        }
    }

    public void RenderSection(PdfSourceSection section)
    {
        _sectionPath = section.RelativeHtmlPath.Replace('\\', '/');
        var html = NormalizeVoidElements(_htmlRenderer.RenderMarkdownFragment(section.Markdown));
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml($"<root>{html}</root>");
        var blocks = document.DocumentElement!.ChildNodes.Cast<XmlNode>().ToArray();
        if (_y > Top)
        {
            EnsureRoom(23 + MinimumSectionStartHeight(blocks));
            _y += 10;
            DrawLine(Left, _y, PageWidth - Right, _y, PdfColor.LightBorder, 0.6);
            _y += 13;
        }

        _destinations[_sectionPath] = new PdfDestination(_pages.Count - 1, PageHeight - _y);
        RenderBlocks(blocks, 0);
    }

    private void RenderBlocks(IEnumerable<XmlNode> nodes, int indent)
    {
        var blocks = nodes.ToArray();
        for (var index = 0; index < blocks.Length; index++)
        {
            if (blocks[index] is XmlElement heading && IsHeading(heading))
            {
                var headingSize = HeadingSize(heading);
                var next = NextContentBlock(blocks, index + 1);
                EnsureRoom(11 + headingSize * 1.35 + 5 + MinimumBlockHeight(next));
            }
            RenderBlock(blocks[index], indent);
        }
    }

    private static XmlNode? NextContentBlock(IReadOnlyList<XmlNode> blocks, int start)
    {
        for (var index = start; index < blocks.Count; index++)
            if (blocks[index] is not XmlText text || !string.IsNullOrWhiteSpace(text.Value))
                return blocks[index];
        return null;
    }

    private static double MinimumBlockHeight(XmlNode? node)
    {
        if (node is not XmlElement element) return node is null ? 0 : 18;
        switch (element.LocalName.ToLowerInvariant())
        {
            case "p": return 4 + 2 * 10.5 * 1.35;
            case "pre": return 16;
            case "table": return 21;
            case "ul": case "ol": return 18;
            case "h1": case "h2": case "h3": case "h4": case "h5": case "h6":
                return 11 + HeadingSize(element) * 1.35 + 5;
            case "figure" when HasClass(element, "diagram"):
                var diagram = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(child => child.LocalName == "svg");
                return diagram is null ? 18 : SvgBlockHeight(diagram, 470, 300) + 10;
            case "div" when HasClass(element, "math-display"):
                var formula = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(child => child.LocalName == "svg");
                return formula is null ? 18 : SvgBlockHeight(formula, 470, 72) + 10;
            default: return 18;
        }
    }

    private static double MinimumSectionStartHeight(IReadOnlyList<XmlNode> blocks)
    {
        var firstIndex = 0;
        while (firstIndex < blocks.Count && blocks[firstIndex] is XmlText text && string.IsNullOrWhiteSpace(text.Value))
            firstIndex++;
        if (firstIndex == blocks.Count) return 0;
        if (blocks[firstIndex] is not XmlElement heading || !IsHeading(heading))
            return MinimumBlockHeight(blocks[firstIndex]);

        var next = NextContentBlock(blocks, firstIndex + 1);
        return 11 + HeadingSize(heading) * 1.35 + 5 + MinimumBlockHeight(next);
    }

    private static double SvgBlockHeight(XmlElement svg, double maxWidth, double maxHeight)
    {
        var dimensions = PdfSvgRenderer.Measure(svg);
        return dimensions.Height * SvgBlockScale(dimensions, maxWidth, maxHeight);
    }

    private static double SvgBlockScale(SvgDimensions dimensions, double maxWidth, double maxHeight) =>
        Math.Min(SvgCssPixelsToPdfPoints,
            Math.Min(maxWidth / Math.Max(dimensions.Width, 1), maxHeight / Math.Max(dimensions.Height, 1)));

    private static bool IsHeading(XmlElement element) =>
        element.LocalName.Length == 2 && element.LocalName[0] == 'h' && element.LocalName[1] is >= '1' and <= '6';

    private static double HeadingSize(XmlElement element) => (element.LocalName[1] - '0') switch
    {
        1 => 22, 2 => 17, 3 => 14, 4 => 12, _ => 11
    };

    private void RenderBlock(XmlNode node, int indent)
    {
        if (node is XmlText text)
        {
            if (!string.IsNullOrWhiteSpace(text.Value))
                WriteParagraph([new PdfRun(CollapseWhitespace(text.Value!), PdfTextStyle.Body)], 10.5, indent, 4, 8);
            return;
        }

        if (node is not XmlElement element) return;
        var name = element.LocalName.ToLowerInvariant();
        switch (name)
        {
            case "h1": case "h2": case "h3": case "h4": case "h5": case "h6":
                var headingSize = HeadingSize(element);
                EnsureRoom(headingSize * 1.35 + 11);
                if (element.HasAttribute("id")) MarkAnchor(element.GetAttribute("id"));
                WriteParagraph(ReadInlineRuns(element.ChildNodes.Cast<XmlNode>(), PdfTextStyle.Heading(headingSize), _sectionPath), headingSize, indent, 11, 5);
                return;
            case "p":
                WriteParagraph(ReadInlineRuns(element.ChildNodes.Cast<XmlNode>(), PdfTextStyle.Body, _sectionPath), 10.5, indent, 4, 8);
                return;
            case "pre":
                RenderCode(element, indent);
                return;
            case "ul": case "ol":
                RenderList(element, indent + 12, name == "ol");
                return;
            case "table":
                RenderTable(element, indent);
                return;
            case "blockquote":
                var quotePage = _pages.Count - 1;
                var quoteTop = _y;
                RenderBlocks(element.ChildNodes.Cast<XmlNode>(), indent + 12);
                DrawAccentAcrossPages(quotePage, quoteTop, Left + indent + 3, PdfColor.Secondary, 2);
                _y += 4;
                return;
            case "button":
                return;
            case "hr":
                _y += 4;
                DrawLine(Left + indent, _y, PageWidth - Right, _y, PdfColor.LightBorder, 0.8);
                _y += 12;
                return;
            case "figure":
                if (HasClass(element, "diagram"))
                {
                    var svg = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(child => child.LocalName == "svg");
                    if (svg is not null) DrawSvgBlock(svg, indent + 8, 470, 300);
                    return;
                }
                RenderBlocks(element.ChildNodes.Cast<XmlNode>(), indent);
                return;
            case "div":
                if (HasClass(element, "icon-search"))
                {
                    WriteParagraph([new PdfRun("[The interactive icon search is available in the HTML version.]", PdfTextStyle.Muted)], 9, indent + 8, 4, 8);
                    return;
                }
                if (HasClass(element, "markdown-alert"))
                {
                    var alertPage = _pages.Count - 1;
                    var alertTop = _y;
                    RenderBlocks(element.ChildNodes.Cast<XmlNode>(), indent + 12);
                    var accent = HasClass(element, "markdown-alert-tip") ? new PdfColor(26, 127, 90) :
                        HasClass(element, "markdown-alert-important") ? new PdfColor(137, 68, 171) :
                        HasClass(element, "markdown-alert-warning") ? new PdfColor(154, 91, 0) :
                        HasClass(element, "markdown-alert-caution") ? new PdfColor(201, 37, 45) : PdfColor.Link;
                    DrawAccentAcrossPages(alertPage, alertTop, Left + indent + 3, accent, 2.5);
                    _y += 4;
                    return;
                }
                if (HasClass(element, "math-display"))
                {
                    var svg = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(child => child.LocalName == "svg");
                    if (svg is not null) DrawSvgBlock(svg, indent + 8, 470, 72);
                    else
                    {
                        var formula = element.GetAttribute("aria-label");
                        if (formula.Length > 0) WriteParagraph([new PdfRun(formula, PdfTextStyle.Math)], 10, indent + 8, 5, 8);
                    }
                    return;
                }
                if (HasClass(element, "media-embed"))
                {
                    var url = element.GetAttribute("data-embed-url");
                    WriteParagraph([new PdfRun(url.Length == 0 ? "[Externe Medien]" : url, PdfTextStyle.Linked(url))], 9.5, indent + 8, 4, 8);
                    return;
                }
                RenderBlocks(element.ChildNodes.Cast<XmlNode>(), indent);
                return;
            case "img":
                RenderImage(element, indent);
                return;
            case "svg":
                DrawSvgBlock(element, indent + 8, 470, 260);
                return;
            default:
                RenderBlocks(element.ChildNodes.Cast<XmlNode>(), indent);
                return;
        }
    }

    private void RenderList(XmlElement list, int indent, bool numbered)
    {
        var number = 1;
        foreach (XmlNode child in list.ChildNodes)
        {
            if (child is not XmlElement { LocalName: "li" } item) continue;
            var prefix = numbered ? $"{number++}. " : "• ";
            var runs = new List<PdfRun> { new(prefix, PdfTextStyle.Body) };
            foreach (XmlNode itemChild in item.ChildNodes)
            {
                if (itemChild is XmlElement { LocalName: "ul" or "ol" } nested)
                {
                    WriteParagraph(runs, 10.5, indent, 2, 2);
                    runs.Clear();
                    RenderList(nested, indent + 13, nested.LocalName == "ol");
                }
                else
                {
                    runs.AddRange(ReadInlineRuns([itemChild], PdfTextStyle.Body, _sectionPath));
                }
            }
            if (runs.Count > 1) WriteParagraph(runs, 10.5, indent, 2, 3);
        }
        _y += 3;
    }

    private void RenderCode(XmlElement pre, int indent)
    {
        var codeRuns = new List<(string Text, PdfColor Color)>();
        ReadCodeRuns(pre, PdfColor.Text, codeRuns);
        var lines = new List<List<(string Text, PdfColor Color)>> { new() };
        foreach (var run in codeRuns)
        {
            var parts = run.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (var part = 0; part < parts.Length; part++)
            {
                if (parts[part].Length > 0) lines[^1].Add((parts[part], run.Color));
                if (part < parts.Length - 1) lines.Add(new List<(string Text, PdfColor Color)>());
            }
        }
        if (lines.Count > 1 && lines[^1].Count == 0) lines.RemoveAt(lines.Count - 1);
        var fontSize = 8.5;
        var lineHeight = 11;
        var codeLeft = Left + indent + 8;
        var maxWidth = PageWidth - Right - codeLeft - 8;
        foreach (var segments in lines)
        {
            var x = codeLeft;
            var hasDrawnLine = false;
            foreach (var segment in segments)
            {
                var remaining = segment.Text;
                while (remaining.Length > 0)
                {
                    if (x >= codeLeft + maxWidth)
                    {
                        _y += lineHeight;
                        x = codeLeft;
                        hasDrawnLine = false;
                    }
                    EnsureRoom(lineHeight + 5);
                    if (!hasDrawnLine)
                        FillRect(codeLeft - 5, _y - 2, maxWidth + 10, lineHeight + 3, PdfColor.CodeBackground);
                    var take = Math.Max(1, Math.Min(remaining.Length, (int)((codeLeft + maxWidth - x) / (fontSize * 0.6))));
                    DrawText(remaining[..take], x, _y + fontSize, fontSize, PdfFont.Courier, segment.Color);
                    x += take * fontSize * 0.6;
                    remaining = remaining[take..];
                    hasDrawnLine = true;
                }
            }
            if (!hasDrawnLine)
            {
                EnsureRoom(lineHeight + 5);
                FillRect(codeLeft - 5, _y - 2, maxWidth + 10, lineHeight + 3, PdfColor.CodeBackground);
            }
            _y += lineHeight;
        }
        _y += 7;
    }

    private static void ReadCodeRuns(XmlNode node, PdfColor color, List<(string Text, PdfColor Color)> runs)
    {
        if (node is XmlText text)
        {
            if (!string.IsNullOrEmpty(text.Value)) runs.Add((text.Value, color));
            return;
        }
        if (node is not XmlElement element) return;
        var className = element.GetAttribute("class");
        var childColor = className switch
        {
            "comment" => new PdfColor(56, 118, 29),
            "keyword" => new PdfColor(5, 80, 174),
            "string" => new PdfColor(163, 21, 21),
            "operator" => new PdfColor(5, 80, 174),
            _ => color
        };
        foreach (XmlNode child in element.ChildNodes) ReadCodeRuns(child, childColor, runs);
    }

    private void RenderTable(XmlElement table, int indent)
    {
        var rows = Descendants(table, "tr").Select(row => row.ChildNodes.OfType<XmlElement>()
            .Where(cell => cell.LocalName is "td" or "th")
            .Select(cell => new TableCell(CollapseWhitespace(cell.InnerText), cell.LocalName == "th"))
            .ToArray()).Where(row => row.Length > 0).ToArray();
        if (rows.Length == 0) return;

        var columns = rows.Max(row => row.Length);
        var width = PageWidth - Left - Right - indent;
        var columnWidths = Enumerable.Range(0, columns)
            .Select(column => Math.Max(50, width * (rows.Max(row => column < row.Length ? Math.Min(45, row[column].Text.Length) : 0) + 12) /
                Enumerable.Range(0, columns).Sum(index => rows.Max(row => index < row.Length ? Math.Min(45, row[index].Text.Length) : 0) + 12)))
            .ToArray();
        var totalWidth = columnWidths.Sum();
        if (totalWidth > width)
            for (var column = 0; column < columns; column++) columnWidths[column] *= width / totalWidth;

        foreach (var row in rows)
        {
            var wrapped = new string[columns][];
            var rowHeight = 12.0;
            for (var column = 0; column < columns; column++)
            {
                var cell = column < row.Length ? row[column] : new TableCell(string.Empty, false);
                wrapped[column] = WrapText(cell.Text, 8.5, columnWidths[column] - 10, PdfFont.Helvetica).ToArray();
                rowHeight = Math.Max(rowHeight, wrapped[column].Length * 10.5 + 8);
            }
            EnsureRoom(rowHeight + 1);
            if (row.Any(cell => cell.Header)) FillRect(Left + indent, _y, width, rowHeight, PdfColor.HeaderBackground);
            DrawLine(Left + indent, _y + rowHeight, Left + indent + width, _y + rowHeight, PdfColor.LightBorder, 0.5);
            var x = Left + indent + 5;
            for (var column = 0; column < columns; column++)
            {
                var cell = column < row.Length ? row[column] : new TableCell(string.Empty, false);
                for (var line = 0; line < wrapped[column].Length; line++)
                    DrawText(wrapped[column][line], x, _y + 10 + line * 10.5, 8.5,
                        cell.Header ? PdfFont.HelveticaBold : PdfFont.Helvetica, PdfColor.Text);
                x += columnWidths[column];
            }
            _y += rowHeight;
        }
        _y += 8;
    }

    private void RenderImage(XmlElement image, int indent)
    {
        var alt = image.GetAttribute("alt");
        var source = image.GetAttribute("src");
        var text = alt.Length == 0 ? "[Bild]" : $"[Bild: {alt}]";
        WriteParagraph([new PdfRun(text, PdfTextStyle.Muted)], 9, indent + 8, 4, 7);
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            WriteParagraph([new PdfRun(source, PdfTextStyle.Linked(source))], 8.5, indent + 8, 0, 7);
    }

    private void DrawSvgBlock(XmlElement svg, int indent, double maxWidth, double maxHeight)
    {
        var size = PdfSvgRenderer.Measure(svg);
        var scale = SvgBlockScale(size, maxWidth, maxHeight);
        var width = size.Width * scale;
        var height = size.Height * scale;
        EnsureRoom(height + 10);
        PdfSvgRenderer.Draw(svg, _page.Content, Left + indent + Math.Max(0, (maxWidth - width) / 2), _y,
            width, height, PageHeight, AddSvgLink);
        _y += height + 8;
    }

    private List<PdfRun> ReadInlineRuns(IEnumerable<XmlNode> nodes, PdfTextStyle style, string sectionPath)
    {
        var runs = new List<PdfRun>();
        foreach (XmlNode node in nodes) ReadInline(node, style, sectionPath, runs);
        return runs;
    }

    private static void ReadInline(XmlNode node, PdfTextStyle style, string sectionPath, List<PdfRun> runs)
    {
        if (node is XmlText text)
        {
            if (text.Value?.Length > 0) runs.Add(new PdfRun(NormalizeInlineWhitespace(text.Value), style));
            return;
        }
        if (node is not XmlElement element) return;
        var name = element.LocalName.ToLowerInvariant();
        if (name == "br")
        {
            runs.Add(new PdfRun("\n", style));
            return;
        }
        if (name == "button" || HasClass(element, "key-symbol")) return;
        if (name == "img")
        {
            var alt = element.GetAttribute("alt");
            runs.Add(new PdfRun(alt.Length == 0 ? "[Bild]" : $"[Bild: {alt}]", PdfTextStyle.Muted));
            var source = element.GetAttribute("src");
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                runs.Add(new PdfRun($" ({source})", PdfTextStyle.Linked(source)));
            return;
        }
        if (name == "input" && element.GetAttribute("type") == "checkbox")
        {
            runs.Add(new PdfRun(element.HasAttribute("checked") ? "[x] " : "[ ] ", style));
            return;
        }
        if (name == "svg")
        {
            runs.Add(new PdfRun(string.Empty, style, element));
            return;
        }
        if (name == "span" && HasClass(element, "emoji"))
        {
            var shortcode = element.GetAttribute("title");
            runs.Add(new PdfRun(shortcode.Length == 0 ? CollapseWhitespace(element.InnerText) : shortcode, style));
            return;
        }

        var childStyle = name switch
        {
            "strong" or "b" => style with { Font = style.Font switch { PdfFont.Helvetica => PdfFont.HelveticaBold, PdfFont.HelveticaItalic => PdfFont.HelveticaBoldItalic, _ => PdfFont.CourierBold } },
            "em" or "i" => style with { Font = style.Font == PdfFont.Helvetica ? PdfFont.HelveticaItalic : style.Font },
            "code" or "kbd" => style with { Font = PdfFont.Courier, Background = PdfColor.CodeBackground },
            "a" => style with { Color = PdfColor.Link, Underline = true, Link = element.GetAttribute("href") },
            "del" or "s" => style with { Strike = true },
            _ => style
        };
        foreach (XmlNode child in element.ChildNodes) ReadInline(child, childStyle, sectionPath, runs);
    }

    private void WriteParagraph(IReadOnlyList<PdfRun> runs, double fontSize, int indent, double before, double after)
    {
        _y += before;
        var startX = Left + indent;
        var x = startX;
        var right = PageWidth - Right;
        var lineHeight = fontSize * 1.35;
        EnsureRoom(lineHeight);
        var lineTop = _y;
        var hasContent = false;
        var lastWasSpace = false;
        foreach (var run in runs)
        {
            if (run.Svg is not null)
            {
                var dimensions = PdfSvgRenderer.Measure(run.Svg);
                var iconHeight = fontSize * 0.9;
                var iconWidth = iconHeight * dimensions.Width / Math.Max(1, dimensions.Height);
                if (x > startX && x + iconWidth > right) NewLine();
                PdfSvgRenderer.Draw(run.Svg, _page.Content, x, lineTop + (lineHeight - iconHeight) / 2,
                    iconWidth, iconHeight, PageHeight, AddSvgLink);
                x += iconWidth + 2;
                hasContent = true;
                continue;
            }
            foreach (var token in Regex.Split(run.Text, "(\\s+)").Where(token => token.Length > 0))
            {
                if (token.Contains('\n'))
                {
                    var pieces = token.Split('\n');
                    for (var piece = 0; piece < pieces.Length; piece++)
                    {
                        if (pieces[piece].Length > 0) PlaceText(pieces[piece], run.Style);
                        if (piece < pieces.Length - 1) NewLine();
                    }
                    lastWasSpace = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    if (x > startX && !lastWasSpace) PlaceText(" ", run.Style);
                    lastWasSpace = true;
                    continue;
                }
                lastWasSpace = false;

                var width = TextWidth(token, fontSize, run.Style.Font);
                if (x > startX && x + width > right)
                    NewLine();
                if (x + width > right && x <= startX)
                {
                    foreach (var line in WrapText(token, fontSize, right - x, run.Style.Font))
                    {
                        PlaceText(line, run.Style);
                        if (line.Length < token.Length) NewLine();
                    }
                }
                else
                {
                    PlaceText(token, run.Style);
                }
            }
        }
        if (hasContent) _y = lineTop + lineHeight;
        _y += after;

        void PlaceText(string text, PdfTextStyle style)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (x == startX && string.IsNullOrWhiteSpace(text)) return;
            var size = style.Size ?? fontSize;
            var width = TextWidth(text, size, style.Font);
            if (style.Background is { } background)
                FillRect(x - 1, lineTop + 1, width + 2, lineHeight - 2, background);
            var baseline = lineTop + size * 0.95;
            DrawText(text, x, baseline, size, style.Font, style.Color);
            if (style.Underline) DrawLine(x, baseline + 1.4, x + width, baseline + 1.4, style.Color, 0.45);
            if (style.Strike) DrawLine(x, baseline - size * 0.3, x + width, baseline - size * 0.3, style.Color, 0.45);
            if (style.Link is { Length: > 0 } link)
            {
                var pdfLink = CreateLink(link, x, PageHeight - (lineTop + lineHeight), x + width, PageHeight - lineTop);
                if (pdfLink is not null) _page.Links.Add(pdfLink);
            }
            x += width;
            hasContent = true;
        }

        void NewLine()
        {
            _y = lineTop + lineHeight;
            EnsureRoom(lineHeight);
            lineTop = _y;
            x = startX;
            hasContent = false;
            lastWasSpace = false;
        }
    }

    private void MarkAnchor(string id) =>
        _destinations[$"{_sectionPath}#{id}"] = new PdfDestination(_pages.Count - 1, PageHeight - _y);

    private PdfLink? CreateLink(string href, double left, double bottom, double right, double top)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto")
            return new PdfLink(left, bottom, right, top, uri.AbsoluteUri, null);

        var baseUri = new Uri($"https://mdu.local/{Uri.EscapeDataString(_sectionPath).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}");
        if (!Uri.TryCreate(baseUri, href, out var target) || target.Host != "mdu.local") return null;
        var path = Uri.UnescapeDataString(target.AbsolutePath.TrimStart('/'));
        var fragment = Uri.UnescapeDataString(target.Fragment.TrimStart('#'));
        var destination = fragment.Length == 0 ? path : $"{path}#{fragment}";
        return new PdfLink(left, bottom, right, top, null, destination);
    }

    private void AddSvgLink(string href, double left, double bottom, double right, double top)
    {
        var link = CreateLink(href, left, bottom, right, top);
        if (link is not null) _page.Links.Add(link);
    }

    private void EnsureRoom(double height)
    {
        if (_y + height <= PageHeight - Bottom) return;
        _page = NewPage();
        _y = Top;
    }

    private PdfPageContent NewPage()
    {
        var page = new PdfPageContent(PageWidth, PageHeight);
        _pages.Add(page);
        return page;
    }

    private void DrawText(string text, double x, double baselineTop, double size, PdfFont font, PdfColor color)
    {
        var y = PageHeight - baselineTop;
        _page.Content.Append(ColorCommand(color, fill: true)).Append("BT /").Append(PdfFileWriter.FontResource(font))
            .Append(' ').Append(F(size)).Append(" Tf 1 0 0 1 ").Append(F(x)).Append(' ').Append(F(y))
            .Append(" Tm <").Append(PdfEncoding.Hex(text)).Append("> Tj ET\n");
    }

    private void FillRect(double x, double top, double width, double height, PdfColor color) =>
        _page.Content.Append(ColorCommand(color, fill: true)).Append(F(x)).Append(' ')
            .Append(F(PageHeight - top - height)).Append(' ').Append(F(width)).Append(' ').Append(F(height)).Append(" re f\n");

    private void DrawLine(double x1, double top1, double x2, double top2, PdfColor color, double width) =>
        _page.Content.Append(ColorCommand(color, fill: false)).Append(F(width)).Append(" w ")
            .Append(F(x1)).Append(' ').Append(F(PageHeight - top1)).Append(" m ")
            .Append(F(x2)).Append(' ').Append(F(PageHeight - top2)).Append(" l S\n");

    private void DrawAccentAcrossPages(int startPage, double startTop, double x, PdfColor color, double width)
    {
        var lastPage = _pages.Count - 1;
        for (var pageIndex = startPage; pageIndex <= lastPage; pageIndex++)
        {
            var top = pageIndex == startPage ? startTop : Top;
            var bottom = pageIndex == lastPage ? _y - 2 : PageHeight - Bottom;
            if (bottom <= top) continue;
            var page = _pages[pageIndex];
            page.Content.Append(ColorCommand(color, fill: false)).Append(F(width)).Append(" w ")
                .Append(F(x)).Append(' ').Append(F(PageHeight - top)).Append(" m ")
                .Append(F(x)).Append(' ').Append(F(PageHeight - bottom)).Append(" l S\n");
        }
    }

    internal static string ColorCommand(PdfColor color, bool fill) =>
        $"{F(color.R / 255.0)} {F(color.G / 255.0)} {F(color.B / 255.0)} {(fill ? "rg" : "RG")} ";

    internal static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static bool HasClass(XmlElement element, string name) =>
        element.GetAttribute("class").Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.Ordinal);
    internal static string CollapseWhitespace(string text) => Regex.Replace(WebUtility.HtmlDecode(text), @"\s+", " ").Trim();
    private static string NormalizeInlineWhitespace(string text) => Regex.Replace(WebUtility.HtmlDecode(text), @"\s+", " ");
    internal static string NormalizeVoidElements(string html)
    {
        var normalized = Regex.Replace(html, @"<(input|br|hr|img)(?<attributes>[^<>]*?)(?<!/)>", "<$1${attributes} />", RegexOptions.IgnoreCase);
        return Regex.Replace(normalized, @"\s(disabled|hidden|checked|selected|required|multiple|data-media-error)(?=[\s/>])", " $1=\"$1\"", RegexOptions.IgnoreCase);
    }
    private static IEnumerable<XmlElement> Descendants(XmlElement element, string name) =>
        element.GetElementsByTagName(name).OfType<XmlElement>();

    internal static IEnumerable<string> WrapText(string text, double size, double maxWidth, PdfFont font)
    {
        var line = new StringBuilder();
        foreach (var token in Regex.Split(text, "(?<=\\s)").Where(token => token.Length > 0))
        {
            var word = token;
            if (line.Length > 0 && TextWidth(line.ToString() + word, size, font) > maxWidth)
            {
                yield return line.ToString().TrimEnd();
                line.Clear();
            }
            while (word.Length > 0 && TextWidth(line.ToString() + word, size, font) > maxWidth)
            {
                var take = Math.Max(1, (int)(word.Length * maxWidth / TextWidth(word, size, font)));
                yield return word[..take];
                word = word[take..];
            }
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString().TrimEnd();
    }

    internal static double TextWidth(string text, double size, PdfFont font)
    {
        if (font is PdfFont.Courier or PdfFont.CourierBold) return text.Length * size * 0.6;
        var widths = font is PdfFont.HelveticaBold or PdfFont.HelveticaBoldItalic
            ? HelveticaBoldWidths
            : HelveticaWidths;
        return text.Sum(character =>
        {
            var baseCharacter = ToBaseCharacter(character);
            return (baseCharacter is >= ' ' and <= '~' ? widths[baseCharacter - ' '] : widths['?' - ' ']) / 1000.0;
        }) * size;
    }

    private static char ToBaseCharacter(char character) => character switch
    {
        'À' or 'Á' or 'Â' or 'Ã' or 'Ä' or 'Å' => 'A',
        'à' or 'á' or 'â' or 'ã' or 'ä' or 'å' => 'a',
        'Ç' => 'C', 'ç' => 'c',
        'È' or 'É' or 'Ê' or 'Ë' => 'E',
        'è' or 'é' or 'ê' or 'ë' => 'e',
        'Ì' or 'Í' or 'Î' or 'Ï' => 'I',
        'ì' or 'í' or 'î' or 'ï' => 'i',
        'Ñ' => 'N', 'ñ' => 'n',
        'Ò' or 'Ó' or 'Ô' or 'Õ' or 'Ö' => 'O',
        'ò' or 'ó' or 'ô' or 'õ' or 'ö' => 'o',
        'Ù' or 'Ú' or 'Û' or 'Ü' => 'U',
        'ù' or 'ú' or 'û' or 'ü' => 'u',
        _ => character
    };

    // Standard Helvetica widths from PDF's 1000-unit text space, used for precise line layout.
    private static readonly int[] HelveticaWidths =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 611, 556, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584
    ];

    private static readonly int[] HelveticaBoldWidths =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584
    ];
}

internal sealed class PdfPageContent(double width, double height)
{
    public double Width { get; } = width;
    public double Height { get; } = height;
    public StringBuilder Content { get; } = new();
    public List<PdfLink> Links { get; } = [];
}

internal sealed record PdfLink(double Left, double Bottom, double Right, double Top, string? Uri, string? Destination);
internal sealed record PdfDestination(int PageIndex, double Top);
internal sealed record PdfLayoutResult(IReadOnlyList<PdfPageContent> Pages, IReadOnlyDictionary<string, PdfDestination> Destinations);
internal sealed record TableCell(string Text, bool Header);
internal sealed record PdfRun(string Text, PdfTextStyle Style, XmlElement? Svg = null);
internal sealed record PdfTextStyle(PdfFont Font, PdfColor Color, double? Size = null, PdfColor? Background = null, bool Underline = false, bool Strike = false, string? Link = null)
{
    public static PdfTextStyle Body { get; } = new(PdfFont.Helvetica, PdfColor.Text);
    public static PdfTextStyle Heading(double size) => new(PdfFont.HelveticaBold, PdfColor.Text, size);
    public static PdfTextStyle Muted { get; } = new(PdfFont.Helvetica, PdfColor.Secondary);
    public static PdfTextStyle Math { get; } = new(PdfFont.HelveticaItalic, PdfColor.Text);
    public static PdfTextStyle Linked(string uri) => new(PdfFont.Helvetica, PdfColor.Link, Link: uri, Underline: true);
}

internal enum PdfFont { Helvetica, HelveticaBold, HelveticaItalic, HelveticaBoldItalic, Courier, CourierBold }
internal readonly record struct PdfColor(byte R, byte G, byte B)
{
    public static PdfColor Text => new(29, 29, 31);
    public static PdfColor Secondary => new(110, 110, 115);
    public static PdfColor Link => new(0, 102, 204);
    public static PdfColor LightBorder => new(220, 220, 225);
    public static PdfColor CodeBackground => new(245, 245, 247);
    public static PdfColor HeaderBackground => new(239, 242, 247);
}

internal static class PdfEncoding
{
    private static readonly Dictionary<char, byte> SpecialCharacters = new()
    {
        ['\u2013'] = (byte)'-', ['\u2014'] = (byte)'-', ['\u2011'] = (byte)'-', ['\u2018'] = 0x91, ['\u2019'] = 0x92,
        ['\u201c'] = 0x93, ['\u201d'] = 0x94, ['\u2022'] = 0x95, ['\u2026'] = 0x85,
        ['\u00a0'] = 0xa0, ['\u2122'] = 0x99
    };

    public static string Hex(string value)
    {
        var bytes = new List<byte>();
        foreach (var character in value)
        {
            if (character <= 0xff && !char.IsControl(character)) bytes.Add((byte)character);
            else if (SpecialCharacters.TryGetValue(character, out var encoded)) bytes.Add(encoded);
            else if (character == '\u2192') { bytes.AddRange(Encoding.ASCII.GetBytes("->")); }
            else if (character == '\u2190') { bytes.AddRange(Encoding.ASCII.GetBytes("<-")); }
            else if (character == '\u2191') { bytes.AddRange(Encoding.ASCII.GetBytes("Up")); }
            else if (character == '\u2193') { bytes.AddRange(Encoding.ASCII.GetBytes("Down")); }
            else if (character == '\u2318') { bytes.AddRange(Encoding.ASCII.GetBytes("Cmd")); }
            else if (character == '\u2325') { bytes.AddRange(Encoding.ASCII.GetBytes("Opt")); }
            else if (character == '\u2303') { bytes.AddRange(Encoding.ASCII.GetBytes("Ctrl")); }
            else if (character == '\u21e7') { bytes.AddRange(Encoding.ASCII.GetBytes("Shift")); }
            else if (character == '\u21a9') { bytes.AddRange(Encoding.ASCII.GetBytes("Return")); }
            else if (character == '\u232b') { bytes.AddRange(Encoding.ASCII.GetBytes("Backspace")); }
            else if (character == '\u2326') { bytes.AddRange(Encoding.ASCII.GetBytes("Delete")); }
            else if (character == '\u2713' || character == '\u2714') bytes.AddRange(Encoding.ASCII.GetBytes("[x]"));
            else if (character == '\u2610') bytes.AddRange(Encoding.ASCII.GetBytes("[ ]"));
            else if (!char.IsControl(character)) bytes.Add((byte)'?');
        }
        return Convert.ToHexString(bytes.ToArray());
    }
}

internal static class PdfFileWriter
{
    private static readonly (PdfFont Font, string Name)[] Fonts =
    [
        (PdfFont.Helvetica, "Helvetica"), (PdfFont.HelveticaBold, "Helvetica-Bold"),
        (PdfFont.HelveticaItalic, "Helvetica-Oblique"), (PdfFont.HelveticaBoldItalic, "Helvetica-BoldOblique"),
        (PdfFont.Courier, "Courier"), (PdfFont.CourierBold, "Courier-Bold")
    ];

    public static string FontResource(PdfFont font) => font switch
    {
        PdfFont.Helvetica => "F1", PdfFont.HelveticaBold => "F2", PdfFont.HelveticaItalic => "F3",
        PdfFont.HelveticaBoldItalic => "F4", PdfFont.Courier => "F5", _ => "F6"
    };

    public static void Write(
        Stream output,
        string title,
        IReadOnlyList<PdfPageContent> pages,
        IReadOnlyDictionary<string, PdfDestination> destinations)
    {
        var objects = new List<byte[]?> { null };
        int Reserve()
        {
            objects.Add(null);
            return objects.Count - 1;
        }
        int Add(string value)
        {
            objects.Add(Encoding.Latin1.GetBytes(value));
            return objects.Count - 1;
        }
        int AddStream(byte[] stream, string extraDictionary = "")
        {
            var prefix = Encoding.ASCII.GetBytes($"<< /Length {stream.Length}{extraDictionary} >>\nstream\n");
            var suffix = Encoding.ASCII.GetBytes("\nendstream");
            var bytes = new byte[prefix.Length + stream.Length + suffix.Length];
            prefix.CopyTo(bytes, 0);
            stream.CopyTo(bytes, prefix.Length);
            suffix.CopyTo(bytes, prefix.Length + stream.Length);
            objects.Add(bytes);
            return objects.Count - 1;
        }

        var catalogId = Reserve();
        var pagesId = Reserve();
        var fontIds = Fonts.Select(font => Add($"<< /Type /Font /Subtype /Type1 /BaseFont /{font.Name} /Encoding /WinAnsiEncoding >>")).ToArray();
        var pageIds = pages.Select(_ => Reserve()).ToArray();
        var contentIds = pages.Select(page => AddStream(Encoding.Latin1.GetBytes(page.Content.ToString()))).ToArray();
        var annotationIds = new int[pages.Count][];
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var page = pages[pageIndex];
            annotationIds[pageIndex] = page.Links.Select(link =>
            {
                var rectangle = $"[{F(link.Left)} {F(link.Bottom)} {F(link.Right)} {F(link.Top)}]";
                if (link.Uri is not null)
                    return Add($"<< /Type /Annot /Subtype /Link /Rect {rectangle} /Border [0 0 0] /A << /S /URI /URI ({PdfString(link.Uri)}) >> >>");
                if (link.Destination is not null && destinations.TryGetValue(link.Destination, out var destination) &&
                    destination.PageIndex >= 0 && destination.PageIndex < pageIds.Length)
                    return Add($"<< /Type /Annot /Subtype /Link /Rect {rectangle} /Border [0 0 0] /Dest [{pageIds[destination.PageIndex]} 0 R /XYZ 0 {F(destination.Top)} null] >>");
                return 0;
            }).Where(id => id > 0).ToArray();
        }

        for (var index = 0; index < pages.Count; index++)
        {
            var annots = annotationIds[index].Length == 0 ? string.Empty : $" /Annots [{string.Join(' ', annotationIds[index].Select(id => $"{id} 0 R"))}]";
            objects[pageIds[index]] = Encoding.ASCII.GetBytes(
                $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {F(pages[index].Width)} {F(pages[index].Height)}] /Resources << /Font << {string.Join(' ', fontIds.Select((id, font) => $"/{FontResource(Fonts[font].Font)} {id} 0 R"))} >> >> /Contents {contentIds[index]} 0 R{annots} >>");
        }
        objects[pagesId] = Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Length} >>");
        objects[catalogId] = Encoding.ASCII.GetBytes($"<< /Type /Catalog /Pages {pagesId} 0 R >>");
        var infoId = Add($"<< /Title ({PdfString(title)}) /Producer (MDU) >>");

        using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.Latin1.GetBytes("%PDF-1.7\n%\xE2\xE3\xCF\xD3\n"));
        var offsets = new long[objects.Count];
        for (var id = 1; id < objects.Count; id++)
        {
            offsets[id] = output.Position;
            writer.Write(Encoding.ASCII.GetBytes($"{id} 0 obj\n"));
            writer.Write(objects[id] ?? throw new InvalidOperationException("A PDF object was not written."));
            writer.Write(Encoding.ASCII.GetBytes("\nendobj\n"));
        }
        var xrefOffset = output.Position;
        writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {objects.Count}\n0000000000 65535 f \n"));
        for (var id = 1; id < objects.Count; id++)
            writer.Write(Encoding.ASCII.GetBytes($"{offsets[id]:D10} 00000 n \n"));
        writer.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size {objects.Count} /Root {catalogId} 0 R /Info {infoId} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n"));
    }

    private static string PdfString(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
