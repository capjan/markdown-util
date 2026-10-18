using System.Text;
using System.Text.Encodings.Web;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdown.Renderer.MarkdigExtension;
using Markdown.Renderer.Res.Model;

namespace Markdown.Renderer;

public class HtmlRenderer
{
    private static readonly Regex TemplateTokenRegex = new(@"%%(?<name>[A-Z_]+)%%", RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new(
        @"<h(?<level>[1-6])(?<attributes>\s[^>]*)?>(?<content>.*?)</h\k<level>>",
        RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex HeadingIdRegex = new(@"\bid=""(?<id>[^""]+)""", RegexOptions.Compiled);
    private readonly MarkdownPipeline _pipeline;
    private readonly LinkReplacer _linkReplacer = new LinkReplacer();
    private string? _basePath;
    private IReadOnlyList<string> _iconSearchAssets = Array.Empty<string>();
    private SiteConfiguration _siteConfiguration = new();

    public HtmlRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseGridTables()
            .UsePipeTables()
            .UseAdvancedExtensions()
            .UseRenderedMath()
            .UseGitHubAlerts()
            .UseIconsAndEmojis()
            .UseKeyboardKeys()
            .UseStaticCodeHighlighter()
            .Build();
    }
    /// <summary>
    /// The icon search files written to the base path because a rendered page contains an icon search.
    /// </summary>
    public IReadOnlyList<string> IconSearchAssets => _iconSearchAssets;

    public void PrepareBasePath(string basePath, SiteConfiguration? siteConfiguration = null)
    {
        _basePath = basePath;
        _siteConfiguration = siteConfiguration ?? new SiteConfiguration();
        _iconSearchAssets = Array.Empty<string>();
        var cssFolder = GetEnsuredCssFolderPath(basePath);
        var cssFilePath = Path.Combine(cssFolder, "main.css");

        WriteCssFileTo(cssFilePath);
    }

    public IReadOnlyCollection<string> FindMediaProviders(string markdown)
    {
        var document = Markdig.Markdown.Parse(_linkReplacer.ReplaceAll(markdown), _pipeline);
        var html = document.ToHtml(_pipeline);
        MediaLinkEmbedder.ReplaceStandaloneLinks(html, out var providers);
        return providers;
    }

    public string RenderMarkdownFragment(string markdown)
    {
        var document = Markdig.Markdown.Parse(_linkReplacer.ReplaceAll(markdown), _pipeline);
        var html = document.ToHtml(_pipeline);
        return MediaLinkEmbedder.ReplaceStandaloneLinks(html, out _);
    }

    public async Task WriteHtml(
        TextWriter writer,
        string title,
        string description,
        int nestingDeep,
        string markdown,
        string editPageLink,
        IReadOnlyList<NavigationItem>? navigationItems = null,
        IReadOnlyList<BreadcrumbItem>? breadcrumbs = null,
        IReadOnlyCollection<string>? siteMediaProviders = null,
        string pageIcon = "")
    {
        var prefix = string.Concat(Enumerable.Repeat("../", nestingDeep));
        var cssFileLink = prefix + "css/main.css";
        var searchScriptLink = prefix + "js/search.js";
        var searchIndexLink = prefix + "search-index.json";
        
        var replacedMarkdown = _linkReplacer.ReplaceAll(markdown);
        
        var document = Markdig.Markdown.Parse(replacedMarkdown, _pipeline);
        var hasIconSearch = document.Descendants<FencedCodeBlock>()
            .Any(block => block.Info == IconSearch.CodeBlockLanguage);
        if (hasIconSearch) EnsureIconSearchAssets();
        var renderedMarkdown = document.ToHtml(_pipeline);
        renderedMarkdown = MediaLinkEmbedder.ReplaceStandaloneLinks(renderedMarkdown, out var mediaProviders);
        if (!string.IsNullOrWhiteSpace(pageIcon))
        {
            var iconName = pageIcon.Trim().Trim(':').Replace('/', '-');
            var iconHtml = Markdig.Markdown.Parse($":{iconName}:", _pipeline).ToHtml(_pipeline)
                .Replace("<p>", string.Empty, StringComparison.Ordinal)
                .Replace("</p>\n", string.Empty, StringComparison.Ordinal)
                .Trim();
            renderedMarkdown = HeadingRegex.Replace(renderedMarkdown, match =>
            {
                if (match.Groups["level"].Value != "1") return match.Value;
                var openEnd = match.Value.IndexOf('>');
                return match.Value.Insert(openEnd + 1, iconHtml + " ");
            }, 1);
        }
        var providersForSettings = siteMediaProviders is { Count: > 0 } ? siteMediaProviders : mediaProviders;
        var tocHeadings = GetTableOfContentsHeadings(renderedMarkdown);
        renderedMarkdown = InsertHeadingPermalinks(renderedMarkdown);
        var breadcrumbMarkup = RenderBreadcrumbs(renderedMarkdown, breadcrumbs ?? Array.Empty<BreadcrumbItem>());
        var model = new MainModel
        {
            Title = title,
            Description = description,
            RenderedMarkdown = renderedMarkdown,
            NavigationItems = navigationItems ?? Array.Empty<NavigationItem>(),
            CssLink = cssFileLink,
            EditPageLink = editPageLink
        };

        var values = new Dictionary<string, string>
        {
            ["TITLE"] = HtmlEncoder.Default.Encode(model.Title),
            ["DESCRIPTION"] = HtmlEncoder.Default.Encode(model.Description),
            ["CSS_LINK"] = HtmlEncoder.Default.Encode(model.CssLink),
            ["SITE_NAME"] = HtmlEncoder.Default.Encode(_siteConfiguration.SiteName),
            ["SITE_BRAND"] = RenderSiteBrand(prefix, _siteConfiguration),
            ["FAVICON_LINK"] = RenderFavicon(prefix, _siteConfiguration),
            ["SITE_THEME_STYLE"] = RenderThemeStyles(_siteConfiguration),
            ["SEARCH_SCRIPT_LINK"] = HtmlEncoder.Default.Encode(searchScriptLink),
            ["SEARCH_INDEX_LINK"] = HtmlEncoder.Default.Encode(searchIndexLink),
            ["TOC_LAYOUT_CLASS"] = " has-on-this-page",
            ["NAVIGATION_ITEMS"] = RenderNavigationItems(model.NavigationItems),
            ["TOP_NAVIGATION_ITEMS"] = RenderTopNavigationItems(model.NavigationItems),
            ["MEDIA_SETTINGS_BUTTON"] = providersForSettings.Count > 0 ? MediaLinkEmbedder.SettingsButtonHtml : string.Empty,
            ["MEDIA_SETTINGS_DIALOG"] = MediaLinkEmbedder.RenderSettingsDialog(providersForSettings),
            ["MARKDOWN"] = model.RenderedMarkdown,
            ["PAGE_TOOLBAR"] = RenderPageToolbar(breadcrumbMarkup, tocHeadings, model.EditPageLink, model.Title),
            ["GITHUB_REPOSITORY_LINK"] = RenderGitHubRepositoryLink(editPageLink),
            ["PAGE_SCRIPTS"] =
                (hasIconSearch
                    ? $"<script defer src=\"{HtmlEncoder.Default.Encode(prefix + "js/" + IconSearch.ScriptFileName)}\"></script>{Environment.NewLine}"
                    : string.Empty)
                + (providersForSettings.Count > 0 ? $"<script>{MediaLinkEmbedder.Script}</script>{Environment.NewLine}" : string.Empty)
        };
        var renderedHtml = TemplateTokenRegex.Replace(Resources.HtmlTemplateContent(), match =>
            values.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value);
        
        await writer.WriteAsync(renderedHtml);
    }

    private void EnsureIconSearchAssets()
    {
        if (_basePath is null || _iconSearchAssets.Count > 0) return;
        _iconSearchAssets = IconSearch.WriteAssetsTo(_basePath);
    }

    private static string RenderSiteBrand(string prefix, SiteConfiguration configuration)
    {
        var name = HtmlEncoder.Default.Encode(configuration.SiteName);
        var light = ResolveAssetUrl(prefix, configuration.LogoLight);
        var dark = ResolveAssetUrl(prefix, configuration.LogoDark);
        var logo = light.Length == 0 ? string.Empty :
            $"<img class=\"site-brand-logo\" src=\"{HtmlEncoder.Default.Encode(light)}\" data-light-src=\"{HtmlEncoder.Default.Encode(light)}\" data-dark-src=\"{HtmlEncoder.Default.Encode(dark)}\" alt=\"\">";
        return $"<a class=\"site-brand\" href=\"{HtmlEncoder.Default.Encode(prefix + "index.html")}\">{logo}<span>{name}</span></a>";
    }

    private static string RenderFavicon(string prefix, SiteConfiguration configuration)
    {
        var light = ResolveAssetUrl(prefix, configuration.FaviconLight);
        if (light.Length == 0) return string.Empty;
        var dark = ResolveAssetUrl(prefix, configuration.FaviconDark);
        return $"<link rel=\"icon\" href=\"{HtmlEncoder.Default.Encode(light)}\" data-light-href=\"{HtmlEncoder.Default.Encode(light)}\" data-dark-href=\"{HtmlEncoder.Default.Encode(dark)}\">";
    }

    private static string ResolveAssetUrl(string prefix, string? asset)
    {
        if (string.IsNullOrWhiteSpace(asset)) return string.Empty;
        if (Uri.TryCreate(asset, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return asset;
        return prefix + string.Join('/', asset.Replace('\\', '/').TrimStart('/').Split('/').Select(Uri.EscapeDataString));
    }

    private static string RenderThemeStyles(SiteConfiguration configuration)
    {
        var light = GetPaletteVariables(configuration.Light);
        var dark = GetPaletteVariables(configuration.Dark);
        if (light.Length == 0 && dark.Length == 0) return string.Empty;
        var css = new StringBuilder("<style>\n");
        if (light.Length > 0) css.Append(":root,:root[data-theme=\"light\"]{").Append(light).Append("}\n");
        if (dark.Length > 0)
        {
            css.Append(":root[data-theme=\"dark\"]{").Append(dark).Append("}\n")
                .Append("@media(prefers-color-scheme:dark){:root[data-theme=\"auto\"]{").Append(dark).Append("}}\n");
        }
        return css.Append("</style>").ToString();
    }

    private static string GetPaletteVariables(SitePalette? palette)
    {
        if (palette is null) return string.Empty;
        var values = new List<string>();
        Add("--page-background", palette.Background);
        Add("--text-primary", palette.Text);
        Add("--link", palette.Primary);
        return string.Join(';', values);

        void Add(string name, string? value)
        {
            if (value is not null && Regex.IsMatch(value, @"^[#a-zA-Z0-9(),.% ]+$")) values.Add($"{name}:{value}");
        }
    }

    private static string RenderGitHubRepositoryLink(string editPageLink)
    {
        if (!Uri.TryCreate(editPageLink, UriKind.Absolute, out var editPageUri) ||
            (editPageUri.Scheme != Uri.UriSchemeHttp && editPageUri.Scheme != Uri.UriSchemeHttps))
            return string.Empty;

        var pathSegments = editPageUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length < 4 || !pathSegments[2].Equals("edit", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var repositoryName = $"{pathSegments[0]}/{pathSegments[1]}";
        var repositoryUrl = $"{editPageUri.GetLeftPart(UriPartial.Authority)}/{repositoryName}";
        var encodedRepositoryUrl = HtmlEncoder.Default.Encode(repositoryUrl);
        var encodedRepositoryName = HtmlEncoder.Default.Encode(repositoryName);

        return $"<a class=\"repository-link\" href=\"{encodedRepositoryUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"GitHub repository: {encodedRepositoryName}\" aria-label=\"GitHub repository: {encodedRepositoryName}\"><svg viewBox=\"0 0 24 24\" aria-hidden=\"true\" focusable=\"false\"><path d=\"M12 .5C5.65.5.5 5.65.5 12c0 5.1 3.31 9.28 7.94 10.78.58.11.79-.25.79-.56v-2.02c-3.23.7-3.91-1.37-3.91-1.37-.53-1.34-1.29-1.7-1.29-1.7-1.05-.72.08-.71.08-.71 1.16.08 1.77 1.19 1.77 1.19 1.03 1.76 2.7 1.25 3.36.96.1-.75.4-1.25.73-1.54-2.58-.29-5.29-1.29-5.29-5.75 0-1.27.45-2.31 1.19-3.13-.12-.29-.52-1.48.11-3.08 0 0 .97-.31 3.17 1.19a11.1 11.1 0 0 1 5.77 0c2.2-1.5 3.17-1.19 3.17-1.19.63 1.6.23 2.79.11 3.08.74.82 1.19 1.86 1.19 3.13 0 4.47-2.72 5.45-5.31 5.74.42.36.78 1.07.78 2.16v3.2c0 .31.21.67.8.56A11.5 11.5 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z\"></path></svg><span>{encodedRepositoryName}</span></a>";
    }

    private static string RenderNavigationItems(IReadOnlyList<NavigationItem> navigationItems)
    {
        var output = new StringBuilder();
        var index = 0;
        RenderItems(0);
        return output.ToString();

        void RenderItems(int depth)
        {
            while (index < navigationItems.Count && navigationItems[index].Depth == depth)
            {
                var itemIndex = index++;
                var item = navigationItems[itemIndex];
                var hasChildren = index < navigationItems.Count && navigationItems[index].Depth > depth;
                var hasToggle = hasChildren && depth > 0;
                var indentDepth = Math.Max(0, item.Depth - 1);
                var encodedTitle = HtmlEncoder.Default.Encode(item.Title);
                var encodedUrl = HtmlEncoder.Default.Encode(item.Url);
                var childrenId = $"site-nav-children-{itemIndex}";
                var expanded = !hasToggle || item.IsExpandedByDefault;
                var itemIndent = new string(' ', 16 + depth * 8);
                var rowIndent = itemIndent + "    ";
                var contentIndent = rowIndent + "    ";

                output.Append(itemIndent).AppendLine($"<li class=\"site-nav-item\" style=\"--indent-depth: {indentDepth}\">");
                output.Append(rowIndent).AppendLine($"<div class=\"site-nav-row{(item.IsCurrent ? " is-current" : string.Empty)}\">");
                if (hasToggle)
                {
                    var label = expanded ? "Collapse" : "Expand";
                    var expandedValue = expanded ? "true" : "false";
                    output.Append(contentIndent).AppendLine($"<button class=\"site-nav-toggle\" type=\"button\" aria-expanded=\"{expandedValue}\" aria-controls=\"{childrenId}\" aria-label=\"{label} {encodedTitle}\"><svg viewBox=\"0 0 12 12\" aria-hidden=\"true\" focusable=\"false\"><path d=\"M3 2l4 4-4 4\"></path></svg></button>");
                }
                else if (depth > 0)
                {
                    output.Append(contentIndent).AppendLine("<span class=\"site-nav-toggle-spacer\" aria-hidden=\"true\"></span>");
                }

                output.Append(contentIndent).AppendLine($"<a class=\"site-nav-link{(item.IsCurrent ? " is-current" : string.Empty)}\" href=\"{encodedUrl}\" aria-current=\"{(item.IsCurrent ? "page" : "false")}\">{encodedTitle}</a>");
                output.Append(rowIndent).AppendLine("</div>");

                if (hasChildren)
                {
                    output.Append(rowIndent).AppendLine($"<ul class=\"site-nav-list site-nav-children\" id=\"{childrenId}\"{(expanded ? string.Empty : " hidden")}>");
                    RenderItems(depth + 1);
                    output.Append(rowIndent).AppendLine("</ul>");
                }

                output.Append(itemIndent).AppendLine("</li>");
            }
        }
    }

    private static string RenderTopNavigationItems(IReadOnlyList<NavigationItem> navigationItems)
    {
        var output = new StringBuilder();
        for (var index = 0; index < navigationItems.Count; index++)
        {
            var item = navigationItems[index];
            if (item.Depth != 1) continue;
            var isCurrentSection = item.IsCurrent;
            for (var child = index + 1; child < navigationItems.Count && navigationItems[child].Depth > 1; child++)
                isCurrentSection |= navigationItems[child].IsCurrent;

            var title = HtmlEncoder.Default.Encode(item.Title);
            var url = HtmlEncoder.Default.Encode(item.Url);
            output.AppendLine($"            <a class=\"top-nav-link{(isCurrentSection ? " is-current" : string.Empty)}\" href=\"{url}\" aria-current=\"{(item.IsCurrent ? "page" : isCurrentSection ? "location" : "false")}\">{title}</a>");
        }

        return output.ToString();
    }

    private static IReadOnlyList<TocHeading> GetTableOfContentsHeadings(string renderedMarkdown)
    {
        var headings = new List<TocHeading>();
        foreach (Match match in HeadingRegex.Matches(renderedMarkdown))
        {
            var level = match.Groups["level"].Value[0] - '0';
            if (level < 2) continue;

            var idMatch = HeadingIdRegex.Match(match.Groups["attributes"].Value);
            if (!idMatch.Success) continue;

            var text = Regex.Replace(match.Groups["content"].Value, "<[^>]+>", string.Empty);
            var title = WebUtility.HtmlDecode(text).Trim();
            if (title.Length == 0) continue;

            headings.Add(new TocHeading(level, idMatch.Groups["id"].Value, title));
        }

        return headings;
    }

    private static string RenderOnThisPage(IReadOnlyList<TocHeading> headings, string editPageLink, string pageTitle)
    {
        var output = new StringBuilder();
        var firstHeading = HtmlEncoder.Default.Encode(headings.Count > 0 ? headings[0].Title : pageTitle);
        output.AppendLine("        <aside class=\"on-this-page\" id=\"on-this-page\" aria-label=\"On this Page\">");
        output.AppendLine("            <h2 class=\"toc-heading\"><span>On this Page</span><svg class=\"toc-progress\" viewBox=\"0 0 20 20\" aria-hidden=\"true\"><circle class=\"toc-progress-track\" cx=\"10\" cy=\"10\" r=\"8\"></circle><circle class=\"toc-progress-value\" cx=\"10\" cy=\"10\" r=\"8\" stroke-dasharray=\"50.265\" stroke-dashoffset=\"50.265\"></circle></svg></h2>");
        output.AppendLine($"            <button class=\"toc-current\" id=\"toc-current\" type=\"button\" aria-expanded=\"false\" aria-controls=\"toc-nav\" aria-label=\"On this Page: {firstHeading}\"><span class=\"toc-current-label\">{firstHeading}</span><svg class=\"toc-progress\" viewBox=\"0 0 20 20\" aria-hidden=\"true\"><circle class=\"toc-progress-track\" cx=\"10\" cy=\"10\" r=\"8\"></circle><circle class=\"toc-progress-value\" cx=\"10\" cy=\"10\" r=\"8\" stroke-dasharray=\"50.265\" stroke-dashoffset=\"50.265\"></circle></svg><svg class=\"toc-chevron\" viewBox=\"0 0 16 16\" aria-hidden=\"true\"><path d=\"m4 6 4 4 4-4\"></path></svg></button>");
        output.AppendLine("            <nav class=\"toc-nav\" id=\"toc-nav\" aria-label=\"On this Page\">");
        if (headings.Count > 0)
        {
            output.AppendLine("                <ul class=\"toc-list\">");
            for (var index = 0; index < headings.Count; index++)
            {
                var heading = headings[index];
                var id = HtmlEncoder.Default.Encode(heading.Id);
                var title = HtmlEncoder.Default.Encode(heading.Title);
                var depth = heading.Level - 2;
                output.AppendLine($"                    <li style=\"--toc-depth: {depth}\"><a class=\"toc-link\" href=\"#{id}\">{title}</a></li>");
            }
            output.AppendLine("                </ul>");
        }
        output.AppendLine("                <div class=\"toc-actions\">");
        if (!string.IsNullOrEmpty(editPageLink))
        {
            output.AppendLine($"                    <a class=\"toc-action\" href=\"{HtmlEncoder.Default.Encode(editPageLink)}\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\"><path d=\"m3 14.5-.8 3.3 3.3-.8L16.8 5.7a1.8 1.8 0 0 0-2.5-2.5L3 14.5Z\"></path><path d=\"m11.8 5.7 2.5 2.5\"></path></svg><span>Edit this page</span></a>");
        }
        output.AppendLine("                    <a class=\"toc-action toc-back-to-top\" href=\"#top\" hidden><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\"><circle cx=\"10\" cy=\"10\" r=\"8\"></circle><path d=\"m6.5 11 3.5-3.5 3.5 3.5\"></path></svg><span>Back to top</span></a>");
        output.AppendLine("                </div>");
        output.AppendLine("            </nav>");
        output.AppendLine("        </aside>");
        return output.ToString();
    }

    private static string RenderPageToolbar(
        string breadcrumbMarkup,
        IReadOnlyList<TocHeading> headings,
        string editPageLink,
        string pageTitle)
    {
        var output = new StringBuilder();
        output.AppendLine("        <div class=\"page-toolbar\">");
        if (breadcrumbMarkup.Length > 0) output.AppendLine($"            {breadcrumbMarkup}");
        output.Append(RenderOnThisPage(headings, editPageLink, pageTitle));
        output.AppendLine("        </div>");
        return output.ToString();
    }

    private sealed record TocHeading(int Level, string Id, string Title);

    private static string RenderBreadcrumbs(
        string renderedMarkdown,
        IReadOnlyList<BreadcrumbItem> breadcrumbs)
    {
        if (breadcrumbs.Count == 0) return string.Empty;
        var headingStart = renderedMarkdown.IndexOf("<h1", StringComparison.Ordinal);
        if (headingStart < 0) return string.Empty;

        const string separator =
            """<span class="breadcrumb-separator" aria-hidden="true">/</span>""";
        var breadcrumbItems = breadcrumbs.Select(item => item.Url is null
            ? $"<span aria-current=\"page\">{HtmlEncoder.Default.Encode(item.Title)}</span>"
            : $"<a href=\"{HtmlEncoder.Default.Encode(item.Url)}\">{HtmlEncoder.Default.Encode(item.Title)}</a>");
        return $"<nav class=\"breadcrumbs\" aria-label=\"Breadcrumb\">{string.Join(separator, breadcrumbItems)}</nav>";
    }

    private static string InsertHeadingPermalinks(string renderedMarkdown) =>
        HeadingRegex.Replace(renderedMarkdown, match =>
        {
            var idMatch = HeadingIdRegex.Match(match.Groups["attributes"].Value);
            if (!idMatch.Success) return match.Value;

            var encodedId = HtmlEncoder.Default.Encode(idMatch.Groups["id"].Value);
            var permalink = $"<a class=\"headerlink\" href=\"#{encodedId}\" title=\"Permanent link\" aria-label=\"Permanent link\">¶</a>";
            var contentEnd = match.Groups["content"].Index + match.Groups["content"].Length - match.Index;
            return match.Value.Insert(contentEnd, permalink);
        });
    
    private string GetEnsuredCssFolderPath(string basePath)
    {
        var cssFolder = Path.Combine(basePath, "css");
        Directory.CreateDirectory(cssFolder);
        return cssFolder;
    }

    private static void WriteCssFileTo(string cssFilePath)
    {
        var mainCssContent = Resources.CssContent();
        var syntaxCssContent = StaticCode.Highlighter.Res.Resources.CssContent();
        
        var cssContentBuilder = new StringBuilder();
        cssContentBuilder.AppendLine(mainCssContent);
        cssContentBuilder.AppendLine(syntaxCssContent);

        var cssContent = cssContentBuilder.ToString();
        File.WriteAllText(cssFilePath, cssContent, Encoding.UTF8);
    }
    


}
