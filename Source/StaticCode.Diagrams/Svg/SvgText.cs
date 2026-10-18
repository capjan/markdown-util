using System.Globalization;
using System.Text;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Text;

namespace StaticCode.Diagrams.Svg;

internal enum TextAlignment
{
    Start,
    Middle
}

/// <summary>Number formatting, escaping, and multi-line text shared by the diagram writers.</summary>
internal static class SvgText
{
    public static string F(double value) =>
        Math.Abs(value) < 0.005 ? "0" : value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    public static string? SafeHref(string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        href = href.Trim();

        var colon = href.IndexOf(':');
        var separator = href.IndexOfAny(new[] { '/', '?', '#' });
        if (colon >= 0 && (separator < 0 || colon < separator))
        {
            var scheme = href[..colon];
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                !scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
                return null;
        }
        else
        {
            var suffixStart = href.IndexOfAny(new[] { '?', '#' });
            var path = suffixStart < 0 ? href : href[..suffixStart];
            var suffix = suffixStart < 0 ? string.Empty : href[suffixStart..];
            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var fileNameStart = path.LastIndexOf('/') + 1;
                var fileName = path[fileNameStart..^3];
                path = fileName.Equals("readme", StringComparison.OrdinalIgnoreCase)
                    ? path[..fileNameStart] + "index.html"
                    : path[..^3] + ".html";
            }

            href = path + suffix;
        }

        return Escape(href);
    }

    /// <summary>Writes lines vertically centered on <paramref name="anchor"/>; <paramref name="anchor"/>.X is the center or the start of each line.</summary>
    public static void WriteText(StringBuilder svg, IReadOnlyList<string> lines, Point anchor, string cssClass, TextAlignment alignment = TextAlignment.Middle)
    {
        // Baselines are computed instead of using dominant-baseline, which renders inconsistently across browsers.
        var firstBaseline = anchor.Y - (lines.Count - 1) / 2.0 * TextMetrics.LineHeight + TextMetrics.FontSize * 0.35;
        var textAnchor = alignment == TextAlignment.Middle ? "middle" : "start";
        if (lines.Count == 1)
        {
            svg.Append($"<text class=\"{cssClass}\" x=\"{F(anchor.X)}\" y=\"{F(firstBaseline)}\" text-anchor=\"{textAnchor}\">{Escape(lines[0])}</text>");
            return;
        }

        svg.Append($"<text class=\"{cssClass}\" text-anchor=\"{textAnchor}\">");
        for (var index = 0; index < lines.Count; index++)
            svg.Append($"<tspan x=\"{F(anchor.X)}\" y=\"{F(firstBaseline + index * TextMetrics.LineHeight)}\">{Escape(lines[index])}</tspan>");
        svg.Append("</text>");
    }
}
