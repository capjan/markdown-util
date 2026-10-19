using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Markdown.Renderer;

/// <summary>
/// The animated hero banner that a <c>hero</c> code block adds to a page. The block holds YAML options;
/// the scene is drawn by a WebGL script that only pages with a hero include.
/// </summary>
public static class Hero
{
    public const string CodeBlockLanguage = "hero";
    public const string FullWidthMarker = "<section class=\"hero hero-full";

    private static readonly Regex HexColorRegex = new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
    private static readonly string[] Times = ["auto", "day", "sunset", "night"];
    private static readonly string[] Qualities = ["auto", "low", "high"];
    private static readonly string[] ColorNames = ["water", "shallow", "sand"];

    public static string Script => Resources.HeroScriptContent();

    public static string Render(string configuration)
    {
        var root = ReadYaml(configuration);
        var width = Choose(ReadScalar(root, "width"), ["full", "content"]) == "full" ? "full" : "inline";
        var align = Choose(ReadScalar(root, "align"), ["left", "center"]);
        var height = (int)Math.Round(ReadNumber(root, "height", 520, 240, 1200));
        var html = new StringBuilder();
        html.Append($"<section class=\"hero hero-{width} hero-align-{align}\" style=\"--hero-height: {height}px\"");
        html.Append(" data-hero-scene=\"beach\"");
        html.Append($" data-hero-time=\"{Choose(ReadScalar(root, "time"), Times)}\"");
        html.Append($" data-hero-quality=\"{Choose(ReadScalar(root, "quality"), Qualities)}\"");
        html.Append($" data-hero-speed=\"{ReadNumber(root, "speed", 1, 0, 4).ToString(CultureInfo.InvariantCulture)}\"");
        html.Append($" data-hero-waves=\"{ReadNumber(root, "waves", 1, 0, 2.5).ToString(CultureInfo.InvariantCulture)}\"");
        if (root?.Children.TryGetValue(new YamlScalarNode("colors"), out var colorsNode) == true && colorsNode is YamlMappingNode colors)
        {
            foreach (var name in ColorNames)
            {
                var color = ReadScalar(colors, name)?.Trim();
                if (color is not null && HexColorRegex.IsMatch(color)) html.Append($" data-hero-{name}=\"{color}\"");
            }
        }
        html.AppendLine(">");
        html.AppendLine("<canvas class=\"hero-canvas\" aria-hidden=\"true\"></canvas>");
        html.AppendLine("<div class=\"hero-content\">");

        var title = ReadScalar(root, "title");
        if (!string.IsNullOrWhiteSpace(title))
            html.AppendLine($"<p class=\"hero-title\">{HtmlEncoder.Default.Encode(title.Trim())}</p>");
        var text = ReadScalar(root, "text");
        if (!string.IsNullOrWhiteSpace(text))
            html.AppendLine($"<p class=\"hero-text\">{HtmlEncoder.Default.Encode(text.Trim())}</p>");

        var actions = ReadActions(root);
        if (actions.Count > 0)
        {
            html.Append("<div class=\"hero-actions\">");
            foreach (var action in actions)
            {
                var external = Uri.TryCreate(action.Link, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
                html.Append($"<a class=\"hero-button{(action.Primary ? " hero-button-primary" : string.Empty)}\" href=\"{HtmlEncoder.Default.Encode(RewriteMarkdownLink(action.Link))}\"");
                if (external) html.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
                html.Append($">{HtmlEncoder.Default.Encode(action.Text)}</a>");
            }
            html.AppendLine("</div>");
        }

        html.AppendLine("</div>");
        html.Append("</section>");
        return html.ToString();
    }

    /// <summary>
    /// Moves the first full-width hero out of the rendered page content so that the page template can place it
    /// above the sidebar and content columns.
    /// </summary>
    public static string ExtractFullWidth(ref string renderedMarkdown)
    {
        var start = renderedMarkdown.IndexOf(FullWidthMarker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        const string end = "</section>";
        var endIndex = renderedMarkdown.IndexOf(end, start, StringComparison.Ordinal);
        if (endIndex < 0) return string.Empty;
        endIndex += end.Length;
        var hero = renderedMarkdown[start..endIndex];
        // Only one hero can span the window; any further ones stay in the page text.
        renderedMarkdown = renderedMarkdown.Remove(start, endIndex - start)
            .Replace(FullWidthMarker, "<section class=\"hero hero-inline", StringComparison.Ordinal);
        return hero;
    }

    private static YamlMappingNode? ReadYaml(string configuration)
    {
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(configuration));
            return stream.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
        }
        catch (YamlDotNet.Core.YamlException)
        {
            return null;
        }
    }

    private static IReadOnlyList<HeroAction> ReadActions(YamlMappingNode? root)
    {
        if (root?.Children.TryGetValue(new YamlScalarNode("actions"), out var node) != true || node is not YamlSequenceNode sequence)
            return Array.Empty<HeroAction>();

        var actions = new List<HeroAction>();
        foreach (var item in sequence.Children.OfType<YamlMappingNode>())
        {
            var text = ReadScalar(item, "text");
            var link = ReadScalar(item, "link");
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(link)) continue;
            var style = ReadScalar(item, "style");
            var primary = style is null ? actions.Count == 0 : style.Equals("primary", StringComparison.OrdinalIgnoreCase);
            actions.Add(new HeroAction(text.Trim(), link.Trim(), primary));
        }

        return actions;
    }

    /// <summary>
    /// Points relative Markdown links at the rendered pages, like links in ordinary Markdown text.
    /// </summary>
    private static string RewriteMarkdownLink(string link)
    {
        if (Uri.TryCreate(link, UriKind.Absolute, out _) || link.StartsWith('/') || link.StartsWith('#')) return link;
        var suffixStart = link.IndexOfAny(['?', '#']);
        var path = suffixStart < 0 ? link : link[..suffixStart];
        var suffix = suffixStart < 0 ? string.Empty : link[suffixStart..];
        if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return link;

        var fileStart = path.LastIndexOf('/') + 1;
        var name = path[fileStart..^3];
        if (name.Equals("readme", StringComparison.OrdinalIgnoreCase)) name = "index";
        return path[..fileStart] + name + ".html" + suffix;
    }

    private static string Choose(string? value, string[] allowed)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is not null && allowed.Contains(normalized) ? normalized : allowed[0];
    }

    private static double ReadNumber(YamlMappingNode? root, string key, double fallback, double min, double max)
    {
        var value = ReadScalar(root, key)?.Trim();
        if (value is not null && value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) value = value[..^2];
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)
            ? Math.Clamp(number, min, max)
            : fallback;
    }

    private static string? ReadScalar(YamlMappingNode? mapping, string key) =>
        mapping?.Children.TryGetValue(new YamlScalarNode(key), out var value) == true && value is YamlScalarNode scalar
            ? scalar.Value
            : null;

    private sealed record HeroAction(string Text, string Link, bool Primary);
}
