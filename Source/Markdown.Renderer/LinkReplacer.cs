using System.Text.RegularExpressions;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Markdown.Renderer;

/// <summary>
/// Points links to Markdown files at the rendered HTML pages. Works on the parsed document,
/// so link syntax shown in code blocks and code spans stays unchanged.
/// </summary>
public class LinkReplacer
{
    private static readonly Regex AbsoluteUrlRegex = new(@"^(?:/|[A-Za-z][A-Za-z0-9+.-]*:)", RegexOptions.Compiled);
    private const string MarkdownExtension = ".md";
    private const string PdfLinkUrl = "mdu:pdf";

    public static bool IsPdfLink(string? url) =>
        string.Equals(url, PdfLinkUrl, StringComparison.OrdinalIgnoreCase);

    public void ReplaceAll(MarkdownDocument document, string? pdfDownloadUrl = null)
    {
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (IsPdfLink(link.Url))
            {
                if (string.IsNullOrWhiteSpace(pdfDownloadUrl)) continue;
                link.Url = pdfDownloadUrl;
                continue;
            }

            link.Url = ReplaceUrl(link.Url);
        }
    }

    public static string? ReplaceUrl(string? url)
    {
        if (string.IsNullOrEmpty(url) || AbsoluteUrlRegex.IsMatch(url)) return url;

        var suffixStart = url.IndexOfAny(['?', '#']);
        var path = suffixStart < 0 ? url : url[..suffixStart];
        var suffix = suffixStart < 0 ? string.Empty : url[suffixStart..];
        if (!path.EndsWith(MarkdownExtension, StringComparison.Ordinal)) return url;

        var fileNameStart = path.LastIndexOf('/') + 1;
        var fileNameWithoutExtension = path[fileNameStart..^MarkdownExtension.Length];
        if (fileNameWithoutExtension.Length == 0) return url;
        if (fileNameWithoutExtension.Equals("readme", StringComparison.InvariantCultureIgnoreCase))
        {
            fileNameWithoutExtension = "index";
        }
        return $"{path[..fileNameStart]}{fileNameWithoutExtension}.html{suffix}";
    }
}
