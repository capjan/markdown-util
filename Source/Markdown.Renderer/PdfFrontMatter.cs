using System.Globalization;

namespace Markdown.Renderer;

internal static class PdfFrontMatter
{
    private static readonly PdfColor Navy = new(31, 45, 68);
    private static readonly PdfColor Accent = new(0, 112, 166);
    private static readonly PdfColor Muted = new(105, 113, 125);
    private static readonly PdfColor Pale = new(239, 244, 249);
    private const double PageWidth = PdfLayout.PageWidth;
    private const double PageHeight = PdfLayout.PageHeight;
    private const double Left = PdfLayout.Left;
    private const double Right = PdfLayout.Right;

    public static PdfFrontMatterResult Create(
        string title,
        string description,
        IReadOnlyList<PdfSourceSection> sections,
        PdfLayoutResult content)
    {
        var cover = CreateCover(title, description, sections.Count);
        var rows = sections.Select(section => CreateRow(section)).ToArray();
        var rowPages = Paginate(rows);
        var tocPages = rowPages.Select((pageRows, index) => CreateContentsPage(pageRows, index + 1, rowPages.Count, content, sections.Count)).ToArray();
        return new PdfFrontMatterResult(new[] { cover }.Concat(tocPages).ToArray());
    }

    private static PdfPageContent CreateCover(string title, string description, int sectionCount)
    {
        var page = new PdfPageContent(PageWidth, PageHeight);
        FillRect(page, 0, 0, PageWidth, 9, Navy);
        FillRect(page, 48, 136, 4, 160, Accent);
        Text(page, "MDU  /  DOCUMENTATION", Left + 18, 158, 9, PdfFont.HelveticaBold, Accent);

        var titleLines = PdfLayout.WrapText(title, 32, PageWidth - Left - Right - 50, PdfFont.HelveticaBold).ToArray();
        var titleTop = 206.0;
        for (var index = 0; index < titleLines.Length; index++)
            Text(page, titleLines[index], Left + 18, titleTop + index * 40, 32, PdfFont.HelveticaBold, Navy);

        var descriptionTop = titleTop + titleLines.Length * 40 + 24;
        var descriptionLines = PdfLayout.WrapText(description, 12, PageWidth - Left - Right - 58, PdfFont.Helvetica).Take(5).ToArray();
        for (var index = 0; index < descriptionLines.Length; index++)
            Text(page, descriptionLines[index], Left + 18, descriptionTop + index * 17, 12, PdfFont.Helvetica, Muted);

        FillRect(page, Left, 696, PageWidth - Left - Right, 1, new PdfColor(222, 228, 235));
        Text(page, $"{sectionCount.ToString(CultureInfo.InvariantCulture)} Kapitel", Left, 722, 10, PdfFont.HelveticaBold, Navy);
        Text(page, "Automatisch aus Markdown erstellt", Left, 741, 9, PdfFont.Helvetica, Muted);
        FillRect(page, PageWidth - Right - 76, 704, 76, 58, Pale);
        Text(page, "MDU", PageWidth - Right - 62, 739, 18, PdfFont.HelveticaBold, Accent);
        return page;
    }

    private static IReadOnlyList<IReadOnlyList<TocRow>> Paginate(IReadOnlyList<TocRow> rows)
    {
        var pages = new List<IReadOnlyList<TocRow>>();
        var pageRows = new List<TocRow>();
        var usedHeight = 0.0;
        foreach (var row in rows)
        {
            if (pageRows.Count > 0 && usedHeight + row.Height > 635)
            {
                pages.Add(pageRows);
                pageRows = [];
                usedHeight = 0;
            }
            pageRows.Add(row);
            usedHeight += row.Height;
        }
        if (pageRows.Count > 0 || pages.Count == 0) pages.Add(pageRows);
        return pages;
    }

    private static PdfPageContent CreateContentsPage(
        IReadOnlyList<TocRow> rows,
        int pageNumber,
        int pageCount,
        PdfLayoutResult content,
        int sectionCount)
    {
        var page = new PdfPageContent(PageWidth, PageHeight);
        FillRect(page, 0, 0, PageWidth, 9, Navy);
        Text(page, "Inhaltsverzeichnis", Left, 69, 23, PdfFont.HelveticaBold, Navy);
        Text(page, $"{sectionCount.ToString(CultureInfo.InvariantCulture)} Kapitel", Left, 94, 9, PdfFont.Helvetica, Muted);
        if (pageCount > 1)
            Text(page, $"{pageNumber.ToString(CultureInfo.InvariantCulture)} / {pageCount.ToString(CultureInfo.InvariantCulture)}", PageWidth - Right - 40, 69, 9, PdfFont.Helvetica, Muted);
        Line(page, Left, 112, PageWidth - Right, 112, new PdfColor(218, 225, 233), 0.8);

        var y = 137.0;
        foreach (var row in rows)
        {
            var x = Left + row.Section.Depth * 14;
            var rowTop = y;
            var size = row.Section.Depth == 0 ? 10.5 : 9.5;
            var font = row.Section.Depth == 0 ? PdfFont.HelveticaBold : PdfFont.Helvetica;
            var pageX = PageWidth - Right - 2;
            var linkRight = pageX + 2;
            for (var lineIndex = 0; lineIndex < row.Lines.Length; lineIndex++)
            {
                var baseline = y + 11 + lineIndex * 14;
                Text(page, row.Lines[lineIndex], x, baseline, size, font, row.Section.Depth == 0 ? Navy : PdfColor.Text);
                if (lineIndex == row.Lines.Length - 1)
                {
                    var textRight = x + PdfLayout.TextWidth(row.Lines[lineIndex], size, font);
                    var leaderStart = textRight + 8;
                    var pageText = content.Destinations.TryGetValue(row.Section.RelativeHtmlPath, out var destination)
                        ? (destination.PageIndex + 1 + pageCount + 1).ToString(CultureInfo.InvariantCulture)
                        : "–";
                    var pageTextWidth = PdfLayout.TextWidth(pageText, 9, PdfFont.HelveticaBold);
                    var pageTextX = pageX - pageTextWidth;
                    if (leaderStart < pageTextX - 8)
                        DottedLine(page, leaderStart, baseline - 3, pageTextX - 7, baseline - 3, new PdfColor(183, 192, 203));
                    Text(page, pageText, pageTextX, baseline, 9, PdfFont.HelveticaBold, Accent);
                }
            }
            y += row.Height;
            page.Links.Add(new PdfLink(x, PageHeight - rowTop - row.Height + 2, linkRight, PageHeight - rowTop + 2,
                null, row.Section.RelativeHtmlPath));
        }
        return page;
    }

    private static TocRow CreateRow(PdfSourceSection section)
    {
        var size = section.Depth == 0 ? 10.5 : 9.5;
        var font = section.Depth == 0 ? PdfFont.HelveticaBold : PdfFont.Helvetica;
        var x = Left + section.Depth * 14;
        var lines = PdfLayout.WrapText(PdfEncoding.PrintableEmojiText(section.Title), size, PageWidth - Right - x - 54, font).ToArray();
        if (lines.Length == 0) lines = [string.Empty];
        return new TocRow(section, lines, Math.Max(22, lines.Length * 14 + 7));
    }

    private static void Text(PdfPageContent page, string value, double x, double baselineTop, double size, PdfFont font, PdfColor color)
    {
        page.Content.Append(PdfLayout.ColorCommand(color, true)).Append("BT /").Append(PdfFileWriter.FontResource(font))
            .Append(' ').Append(PdfLayout.F(size)).Append(" Tf 1 0 0 1 ").Append(PdfLayout.F(x)).Append(' ')
            .Append(PdfLayout.F(PageHeight - baselineTop)).Append(" Tm <").Append(PdfEncoding.Hex(value)).Append("> Tj ET\n");
    }

    private static void FillRect(PdfPageContent page, double x, double top, double width, double height, PdfColor color) =>
        page.Content.Append(PdfLayout.ColorCommand(color, true)).Append(PdfLayout.F(x)).Append(' ')
            .Append(PdfLayout.F(PageHeight - top - height)).Append(' ').Append(PdfLayout.F(width)).Append(' ')
            .Append(PdfLayout.F(height)).Append(" re f\n");

    private static void Line(PdfPageContent page, double x1, double top1, double x2, double top2, PdfColor color, double width) =>
        page.Content.Append(PdfLayout.ColorCommand(color, false)).Append(PdfLayout.F(width)).Append(" w ")
            .Append(PdfLayout.F(x1)).Append(' ').Append(PdfLayout.F(PageHeight - top1)).Append(" m ")
            .Append(PdfLayout.F(x2)).Append(' ').Append(PdfLayout.F(PageHeight - top2)).Append(" l S\n");

    private static void DottedLine(PdfPageContent page, double x1, double top1, double x2, double top2, PdfColor color) =>
        page.Content.Append("[1 2] 0 d ").Append(PdfLayout.ColorCommand(color, false)).Append("0.6 w ")
            .Append(PdfLayout.F(x1)).Append(' ').Append(PdfLayout.F(PageHeight - top1)).Append(" m ")
            .Append(PdfLayout.F(x2)).Append(' ').Append(PdfLayout.F(PageHeight - top2)).Append(" l S [] 0 d\n");

    private sealed record TocRow(PdfSourceSection Section, string[] Lines, double Height);
}

internal sealed record PdfFrontMatterResult(IReadOnlyList<PdfPageContent> Pages);
