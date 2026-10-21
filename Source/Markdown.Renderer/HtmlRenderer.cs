using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
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
    private static readonly Regex FootnoteReferenceRegex = new(
        @"<a\b[^>]*\bclass=""footnote-ref""[^>]*>.*?</a>",
        RegexOptions.Compiled | RegexOptions.Singleline);
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
            .UseScrollableTables()
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

    private MarkdownDocument ParseMarkdown(string markdown, string? pdfDownloadUrl = null)
    {
        var document = Markdig.Markdown.Parse(markdown, _pipeline);
        _linkReplacer.ReplaceAll(document, pdfDownloadUrl);
        return document;
    }

    public bool ContainsPdfLink(string markdown) =>
        Markdig.Markdown.Parse(markdown, _pipeline)
            .Descendants<LinkInline>()
            .Any(link => LinkReplacer.IsPdfLink(link.Url));

    public IReadOnlyCollection<string> FindMediaProviders(string markdown)
    {
        var document = ParseMarkdown(markdown);
        var html = document.ToHtml(_pipeline);
        MediaLinkEmbedder.ReplaceStandaloneLinks(html, out var providers);
        return providers;
    }

    public string RenderMarkdownFragment(string markdown)
    {
        var document = ParseMarkdown(markdown);
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
        string pageIcon = "",
        IReadOnlyList<NavigationItem>? topNavigationItems = null,
        DateTimeOffset? lastModified = null,
        string? pdfDownloadUrl = null)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", nestingDeep));
        var cssFileLink = prefix + "css/main.css";
        var searchScriptLink = prefix + "js/search.js";
        var searchIndexLink = prefix + "search-index.json";
        
        var document = ParseMarkdown(markdown, pdfDownloadUrl);
        var hasIconSearch = document.Descendants<FencedCodeBlock>()
            .Any(block => block.Info == IconSearch.CodeBlockLanguage);
        if (hasIconSearch) EnsureIconSearchAssets();
        var hasHero = document.Descendants<FencedCodeBlock>()
            .Any(block => block.Info == Hero.CodeBlockLanguage);
        var renderedMarkdown = document.ToHtml(_pipeline);
        renderedMarkdown = MediaLinkEmbedder.ReplaceStandaloneLinks(renderedMarkdown, out var mediaProviders);
        var pageHero = hasHero ? Hero.ExtractFullWidth(ref renderedMarkdown) : string.Empty;
        renderedMarkdown = InsertFootnotesTitle(renderedMarkdown);
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
        var pageHeading = GetPageHeadingTitle(renderedMarkdown);
        renderedMarkdown = InsertHeadingPermalinks(renderedMarkdown);
        renderedMarkdown = InsertEditPageLink(renderedMarkdown, editPageLink, out var editLinkInserted);
        var breadcrumbMarkup = RenderBreadcrumbs(renderedMarkdown, breadcrumbs ?? Array.Empty<BreadcrumbItem>());
        var model = new MainModel
        {
            Title = title,
            Description = description,
            RenderedMarkdown = renderedMarkdown,
            NavigationItems = navigationItems ?? Array.Empty<NavigationItem>(),
            TopNavigationItems = topNavigationItems ?? Array.Empty<NavigationItem>(),
            CssLink = cssFileLink,
            EditPageLink = editPageLink
        };
        var hasTopNavigation = model.TopNavigationItems.Count > 0;
        var hasCurrentTopSection = model.NavigationItems.Any(item =>
            item.IsTopNavigationItem && item.IsInCurrentTopSection);
        var currentTopNavigationItem = model.TopNavigationItems.FirstOrDefault(item => item.IsCurrent || item.IsCurrentSection);
        var hasWideSidebar = !hasTopNavigation || (hasCurrentTopSection
            ? model.NavigationItems.Any(item => item.IsInCurrentTopSection && !item.IsTopNavigationItem)
            : model.NavigationItems.Any(item => item.IsCurrent && item.Depth > 0));

        var values = new Dictionary<string, string>
        {
            ["TITLE"] = HtmlEncoder.Default.Encode(model.Title),
            ["DESCRIPTION"] = HtmlEncoder.Default.Encode(model.Description),
            ["CSS_LINK"] = HtmlEncoder.Default.Encode(model.CssLink),
            ["SITE_NAME"] = HtmlEncoder.Default.Encode(_siteConfiguration.SiteName),
            ["SITE_BRAND"] = RenderSiteBrand(prefix, _siteConfiguration, pageHeading),
            ["FAVICON_LINK"] = RenderFavicon(prefix, _siteConfiguration),
            ["SITE_THEME_STYLE"] = RenderThemeStyles(_siteConfiguration),
            ["SEARCH_SCRIPT_LINK"] = HtmlEncoder.Default.Encode(searchScriptLink),
            ["SEARCH_INDEX_LINK"] = HtmlEncoder.Default.Encode(searchIndexLink),
            ["TOC_LAYOUT_CLASS"] = " has-on-this-page",
            ["NAVIGATION_ITEMS"] = RenderNavigationItems(model.NavigationItems),
            ["TOP_NAVIGATION_ITEMS"] = RenderTopNavigationItems(model.TopNavigationItems),
            ["SIDEBAR_SECTION_TITLE"] = currentTopNavigationItem is not null && hasWideSidebar
                ? $"<div class=\"site-nav-section-title\">{HtmlEncoder.Default.Encode(currentTopNavigationItem.Title)}</div>"
                : string.Empty,
            ["TOP_NAVIGATION_CLASS"] = hasTopNavigation ? string.Empty : " is-empty",
            ["SITE_LAYOUT_CLASS"] = hasTopNavigation
                ? hasWideSidebar
                    ? hasCurrentTopSection
                        ? " has-top-navigation has-current-top-section has-wide-sidebar"
                        : " has-top-navigation has-wide-sidebar"
                    : " has-top-navigation no-wide-sidebar"
                : " has-wide-sidebar",
            ["MEDIA_SETTINGS_BUTTON"] = providersForSettings.Count > 0 ? MediaLinkEmbedder.SettingsButtonHtml : string.Empty,
            ["MEDIA_SETTINGS_DIALOG"] = MediaLinkEmbedder.RenderSettingsDialog(providersForSettings),
            ["MARKDOWN"] = model.RenderedMarkdown,
            ["PAGE_HERO"] = pageHero,
            ["PAGE_STYLES"] = hasHero ? $"<style>{Hero.Styles}</style>" : string.Empty,
            ["PAGE_BREADCRUMBS"] = breadcrumbMarkup,
            ["PAGE_TOC"] = RenderOnThisPage(tocHeadings),
            ["PAGE_FOOTER"] = RenderPageFooter(model.NavigationItems, editPageLink, !editLinkInserted, lastModified),
            ["BACK_TO_TOP"] = "<a class=\"back-to-top\" id=\"back-to-top\" href=\"#top\" aria-label=\"Back to top\" title=\"Back to top\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\"><path d=\"M10 16V4m-5 5 5-5 5 5\"></path></svg><span>Back to top</span></a>",
            ["GITHUB_REPOSITORY_LINK"] = RenderGitHubRepositoryLink(editPageLink),
            ["PAGE_SCRIPTS"] =
                (hasIconSearch
                    ? $"<script defer src=\"{HtmlEncoder.Default.Encode(prefix + "js/" + IconSearch.ScriptFileName)}\"></script>{Environment.NewLine}"
                    : string.Empty)
                + (providersForSettings.Count > 0 ? $"<script>{MediaLinkEmbedder.Script}</script>{Environment.NewLine}" : string.Empty)
                + (hasHero ? $"<script>{Hero.Script}</script>{Environment.NewLine}" : string.Empty)
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

    private static string RenderSiteBrand(string prefix, SiteConfiguration configuration, string pageHeading)
    {
        var name = HtmlEncoder.Default.Encode(configuration.SiteName);
        var heading = HtmlEncoder.Default.Encode(pageHeading.Length > 0 ? pageHeading : configuration.SiteName);
        var light = ResolveAssetUrl(prefix, configuration.LogoLight);
        var dark = ResolveAssetUrl(prefix, configuration.LogoDark);
        var logo = light.Length == 0 ? string.Empty :
            $"<img class=\"site-brand-logo\" src=\"{HtmlEncoder.Default.Encode(light)}\" data-light-src=\"{HtmlEncoder.Default.Encode(light)}\" data-dark-src=\"{HtmlEncoder.Default.Encode(dark)}\" alt=\"\">";
        return $"<div class=\"site-header-leading\"><div class=\"site-header-icon-slot{(light.Length > 0 ? " has-logo" : string.Empty)}\">{logo}<button class=\"navigation-toggle\" id=\"navigation-toggle\" type=\"button\" aria-expanded=\"true\" aria-controls=\"site-navigation\" aria-label=\"Collapse navigation\" title=\"Collapse navigation\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\" focusable=\"false\"><rect x=\"2.5\" y=\"3.5\" width=\"15\" height=\"13\" rx=\"2\"></rect><path d=\"M8 4v12M5 7h1M5 10h1M5 13h1\"></path></svg></button></div><a class=\"site-brand\" href=\"{HtmlEncoder.Default.Encode(prefix + "index.html")}\"><span id=\"site-brand-title\" data-site-title=\"{name}\" data-page-title=\"{heading}\">{name}</span></a></div>";
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
        Add("--surface-header", palette.Background);
        Add("--text-primary", palette.Text);
        Add("--link", palette.Primary);
        // Secondary text, lines and surfaces follow a custom palette instead of staying on the default neutrals.
        if (palette.Background is not null || palette.Text is not null)
        {
            values.Add($"--text-secondary:{MixText(63)}");
            values.Add($"--border:{MixText(16)}");
            values.Add($"--border-subtle:{MixText(11)}");
            values.Add($"--header-divider:{MixText(11)}");
            values.Add($"--code-block-border:{MixText(11)}");
            values.Add($"--surface-current:{MixText(5)}");
            values.Add($"--surface-code:{MixText(8)}");
            values.Add($"--surface-code-block:{MixText(5)}");
            values.Add($"--surface-code-block-body:{MixText(2)}");
        }
        if (palette.Primary is not null) values.Add("--nav-hover:color-mix(in oklab,var(--link) 16%,var(--page-background))");
        return string.Join(';', values);

        static string MixText(int percent) =>
            $"color-mix(in oklab,var(--text-primary) {percent}%,var(--page-background))";

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

                var itemClass = item.IsTopNavigationItem ? "site-nav-item top-navigation-entry" : "site-nav-item";
                if (item.IsCurrentSection) itemClass += " is-current-section";
                if (item.IsInCurrentTopSection) itemClass += " is-in-current-top-section";
                if (item.IsAncestorOfCurrentTopSection) itemClass += " is-top-navigation-ancestor";
                output.Append(itemIndent).AppendLine($"<li class=\"{itemClass}\" style=\"--indent-depth: {indentDepth}; --wide-indent-depth: {Math.Max(0, item.TopNavigationDepth)}\">");
                var rowClass = item.IsCurrent ? " is-current" : item.IsCurrentSection ? " is-current-section" : string.Empty;
                var currentValue = item.IsCurrent ? "page" : item.IsCurrentSection ? "location" : "false";
                output.Append(rowIndent).AppendLine($"<div class=\"site-nav-row{rowClass}\">");
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

                output.Append(contentIndent).AppendLine($"<a class=\"site-nav-link{(item.IsCurrent ? " is-current" : item.IsCurrentSection ? " is-current-section" : string.Empty)}\" href=\"{encodedUrl}\" aria-current=\"{currentValue}\">{encodedTitle}</a>");
                output.Append(rowIndent).AppendLine("</div>");

                if (hasChildren)
                {
                    var childClass = item.IsTopNavigationItem ? "site-nav-list site-nav-children top-navigation-children" : "site-nav-list site-nav-children";
                    output.Append(rowIndent).AppendLine($"<ul class=\"{childClass}\" id=\"{childrenId}\"{(expanded ? string.Empty : " hidden")}>");
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
        foreach (var item in navigationItems)
        {
            var title = HtmlEncoder.Default.Encode(item.Title);
            var url = HtmlEncoder.Default.Encode(item.Url);
            var isCurrentSection = item.IsCurrent || item.IsCurrentSection;
            output.AppendLine($"            <a class=\"top-nav-link{(isCurrentSection ? " is-current" : string.Empty)}\" href=\"{url}\" aria-current=\"{(item.IsCurrent ? "page" : isCurrentSection ? "location" : "false")}\" data-label=\"{title}\">{title}</a>");
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

            var title = GetHeadingText(match.Groups["content"].Value);
            if (title.Length == 0) continue;

            headings.Add(new TocHeading(level, idMatch.Groups["id"].Value, title));
        }

        return headings;
    }

    private static string RenderOnThisPage(IReadOnlyList<TocHeading> headings)
    {
        if (headings.Count == 0) return string.Empty;
        var output = new StringBuilder();
        output.AppendLine("        <aside class=\"on-this-page\" id=\"on-this-page\" aria-label=\"On this Page\">");
        output.AppendLine("            <h2 class=\"toc-heading\">On this Page</h2>");
        output.AppendLine("            <nav class=\"toc-nav\" id=\"toc-nav\" aria-label=\"On this Page\">");
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
        output.AppendLine("            </nav>");
        output.AppendLine("        </aside>");
        return output.ToString();
    }

    private sealed record TocHeading(int Level, string Id, string Title);

    // The heading has no id, so it stays out of the page contents and gets no permalink.
    private static string InsertFootnotesTitle(string renderedMarkdown) => renderedMarkdown.Replace(
        "<div class=\"footnotes\">\n<hr />\n",
        "<div class=\"footnotes\">\n<hr />\n<h2 class=\"footnotes-title\">Footnotes</h2>\n",
        StringComparison.Ordinal);

    private static string RenderPageFooter(
        IReadOnlyList<NavigationItem> navigationItems,
        string editPageLink,
        bool showEditPageLink,
        DateTimeOffset? lastModified)
    {
        var meta = new List<string>();
        if (lastModified is { } date)
        {
            var isoDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var displayDate = date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            meta.Add($"<span>Last updated on <time datetime=\"{isoDate}\">{displayDate}</time></span>");
        }
        if (showEditPageLink && !string.IsNullOrWhiteSpace(editPageLink))
            meta.Add($"<a href=\"{HtmlEncoder.Default.Encode(editPageLink)}\" target=\"_blank\" rel=\"noopener noreferrer\">Edit this page</a>");

        // The sidebar order is the reading order, so its neighbors are the previous and next pages.
        var currentIndex = -1;
        for (var index = 0; index < navigationItems.Count; index++)
        {
            if (!navigationItems[index].IsCurrent) continue;
            currentIndex = index;
            break;
        }
        var previous = currentIndex > 0 ? navigationItems[currentIndex - 1] : null;
        var next = currentIndex >= 0 && currentIndex < navigationItems.Count - 1 ? navigationItems[currentIndex + 1] : null;
        if (meta.Count == 0 && previous is null && next is null) return string.Empty;

        var output = new StringBuilder();
        output.AppendLine("<footer class=\"page-footer\">");
        if (meta.Count > 0)
            output.AppendLine($"    <div class=\"page-meta\">{string.Join(string.Empty, meta)}</div>");
        if (previous is not null || next is not null)
        {
            output.AppendLine("    <nav class=\"page-pagination\" aria-label=\"Previous and next page\">");
            if (previous is not null)
                output.AppendLine(RenderPaginationLink(previous, "is-previous", "prev", "Previous"));
            if (next is not null)
                output.AppendLine(RenderPaginationLink(next, "is-next", "next", "Next"));
            output.AppendLine("    </nav>");
        }
        output.AppendLine("</footer>");
        return output.ToString();

        static string RenderPaginationLink(NavigationItem item, string className, string rel, string label) =>
            $"        <a class=\"page-pagination-link {className}\" href=\"{HtmlEncoder.Default.Encode(item.Url)}\" rel=\"{rel}\">" +
            $"<span class=\"page-pagination-label\">{label}</span>" +
            $"<span class=\"page-pagination-title\">{HtmlEncoder.Default.Encode(item.Title)}</span></a>";
    }

    private static string RenderBreadcrumbs(
        string renderedMarkdown,
        IReadOnlyList<BreadcrumbItem> breadcrumbs)
    {
        // The current page is left out because the page heading below the trail already names it.
        var ancestors = breadcrumbs.Where(item => item.Url is not null).ToList();
        if (ancestors.Count == 0) return string.Empty;
        var headingStart = renderedMarkdown.IndexOf("<h1", StringComparison.Ordinal);
        if (headingStart < 0) return string.Empty;

        const string separator =
            """<span class="breadcrumb-separator" aria-hidden="true">/</span>""";
        var breadcrumbItems = ancestors.Select(item =>
            $"<a href=\"{HtmlEncoder.Default.Encode(item.Url!)}\">{HtmlEncoder.Default.Encode(item.Title)}</a>");
        return $"<nav class=\"breadcrumbs\" aria-label=\"Breadcrumb\">{string.Join(separator, breadcrumbItems)}</nav>";
    }

    private static string GetPageHeadingTitle(string renderedMarkdown)
    {
        var heading = HeadingRegex.Match(renderedMarkdown);
        if (!heading.Success || heading.Groups["level"].Value != "1") return string.Empty;
        return GetHeadingText(heading.Groups["content"].Value);
    }

    private static string GetHeadingText(string headingContent)
    {
        var text = FootnoteReferenceRegex.Replace(headingContent, string.Empty);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        return WebUtility.HtmlDecode(text).Trim();
    }

    private static string InsertHeadingPermalinks(string renderedMarkdown) =>
        HeadingRegex.Replace(renderedMarkdown, match =>
        {
            // The page URL already links to the page heading.
            if (match.Groups["level"].Value == "1") return match.Value;
            var idMatch = HeadingIdRegex.Match(match.Groups["attributes"].Value);
            if (!idMatch.Success) return match.Value;

            var encodedId = HtmlEncoder.Default.Encode(idMatch.Groups["id"].Value);
            var permalink = $"<a class=\"headerlink\" href=\"#{encodedId}\" title=\"Permanent link\" aria-label=\"Permanent link\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\" focusable=\"false\"><path d=\"M8.5 6H6a4 4 0 0 0 0 8h2.5M11.5 6H14a4 4 0 0 1 0 8h-2.5M7 10h6\" transform=\"rotate(-45 10 10)\"></path></svg></a>";
            var contentEnd = match.Groups["content"].Index + match.Groups["content"].Length - match.Index;
            return match.Value.Insert(contentEnd, permalink);
        });

    private static string InsertEditPageLink(string renderedMarkdown, string editPageLink, out bool inserted)
    {
        inserted = false;
        if (string.IsNullOrWhiteSpace(editPageLink)) return renderedMarkdown;
        var editLinkInserted = false;
        var result = HeadingRegex.Replace(renderedMarkdown, match =>
        {
            if (editLinkInserted || match.Groups["level"].Value != "1") return match.Value;
            editLinkInserted = true;
            var editLink = $"<a class=\"page-edit-link\" href=\"{HtmlEncoder.Default.Encode(editPageLink)}\" target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"Edit this page\" title=\"Edit this page\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\"><path d=\"m3 14.5-.8 3.3 3.3-.8L16.8 5.7a1.8 1.8 0 0 0-2.5-2.5L3 14.5Z\"></path><path d=\"m11.8 5.7 2.5 2.5\"></path></svg></a>";
            var contentEnd = match.Groups["content"].Index + match.Groups["content"].Length - match.Index;
            return match.Value.Insert(contentEnd, editLink);
        });
        inserted = editLinkInserted;
        return result;
    }
    
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
