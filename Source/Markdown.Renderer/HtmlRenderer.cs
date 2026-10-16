using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdown.Renderer.MarkdigExtension;
using Markdown.Renderer.Res.Model;

namespace Markdown.Renderer;

public class HtmlRenderer
{
    private static readonly Regex TemplateTokenRegex = new(@"%%(?<name>[A-Z_]+)%%", RegexOptions.Compiled);
    private readonly MarkdownPipeline _pipeline;
    private readonly LinkReplacer _linkReplacer = new LinkReplacer();
    private string? _basePath;
    private IReadOnlyList<string> _iconSearchAssets = Array.Empty<string>();

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

    public void PrepareBasePath(string basePath)
    {
        _basePath = basePath;
        _iconSearchAssets = Array.Empty<string>();
        var cssFolder = GetEnsuredCssFolderPath(basePath);
        var cssFilePath = Path.Combine(cssFolder, "main.css");

        WriteCssFileTo(cssFilePath);
    }
    public async Task WriteHtml(
        TextWriter writer,
        string title,
        string description,
        int nestingDeep,
        string markdown,
        string editPageLink,
        IReadOnlyList<NavigationItem>? navigationItems = null,
        IReadOnlyList<BreadcrumbItem>? breadcrumbs = null)
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
        renderedMarkdown = InsertBreadcrumbsAboveHeading(renderedMarkdown, breadcrumbs ?? Array.Empty<BreadcrumbItem>());
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
            ["SEARCH_SCRIPT_LINK"] = HtmlEncoder.Default.Encode(searchScriptLink),
            ["SEARCH_INDEX_LINK"] = HtmlEncoder.Default.Encode(searchIndexLink),
            ["NAVIGATION_ITEMS"] = RenderNavigationItems(model.NavigationItems),
            ["MARKDOWN"] = model.RenderedMarkdown,
            ["EDIT_PAGE_LINK"] = RenderEditPageLink(model.EditPageLink),
            ["PAGE_SCRIPTS"] = hasIconSearch
                ? $"<script defer src=\"{HtmlEncoder.Default.Encode(prefix + "js/" + IconSearch.ScriptFileName)}\"></script>{Environment.NewLine}"
                : string.Empty
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
                var itemIndent = new string(' ', 16 + depth * 8);
                var rowIndent = itemIndent + "    ";
                var contentIndent = rowIndent + "    ";

                output.Append(itemIndent).AppendLine($"<li class=\"site-nav-item\" style=\"--indent-depth: {indentDepth}\">");
                output.Append(rowIndent).AppendLine($"<div class=\"site-nav-row{(item.IsCurrent ? " is-current" : string.Empty)}\">");
                if (hasToggle)
                {
                    output.Append(contentIndent).AppendLine($"<button class=\"site-nav-toggle\" type=\"button\" aria-expanded=\"true\" aria-controls=\"{childrenId}\" aria-label=\"Collapse {encodedTitle}\"><svg viewBox=\"0 0 12 12\" aria-hidden=\"true\" focusable=\"false\"><path d=\"M3 2l4 4-4 4\"></path></svg></button>");
                }
                else if (depth > 0)
                {
                    output.Append(contentIndent).AppendLine("<span class=\"site-nav-toggle-spacer\" aria-hidden=\"true\"></span>");
                }

                output.Append(contentIndent).AppendLine($"<a class=\"site-nav-link{(item.IsCurrent ? " is-current" : string.Empty)}\" href=\"{encodedUrl}\" aria-current=\"{(item.IsCurrent ? "page" : "false")}\">{encodedTitle}</a>");
                output.Append(rowIndent).AppendLine("</div>");

                if (hasChildren)
                {
                    output.Append(rowIndent).AppendLine($"<ul class=\"site-nav-list site-nav-children\" id=\"{childrenId}\">");
                    RenderItems(depth + 1);
                    output.Append(rowIndent).AppendLine("</ul>");
                }

                output.Append(itemIndent).AppendLine("</li>");
            }
        }
    }

    private static string RenderEditPageLink(string editPageLink) =>
        string.IsNullOrEmpty(editPageLink)
            ? string.Empty
            : $$"""
              <hr />
              <a href="{{HtmlEncoder.Default.Encode(editPageLink)}}">✎ Edit Page</a>
              """;

    private static string InsertBreadcrumbsAboveHeading(
        string renderedMarkdown,
        IReadOnlyList<BreadcrumbItem> breadcrumbs)
    {
        if (breadcrumbs.Count == 0) return renderedMarkdown;
        var headingStart = renderedMarkdown.IndexOf("<h1", StringComparison.Ordinal);
        if (headingStart < 0) return renderedMarkdown;

        const string separatorIcon =
            """<span class="breadcrumb-separator" aria-hidden="true"><svg viewBox="0 0 12 20" focusable="false"><path d="M2 2L9 10L2 18" /></svg></span>""";
        var breadcrumbItems = breadcrumbs.Select(item => item.Url is null
            ? $"<span aria-current=\"page\">{HtmlEncoder.Default.Encode(item.Title)}</span>"
            : $"<a href=\"{HtmlEncoder.Default.Encode(item.Url)}\">{HtmlEncoder.Default.Encode(item.Title)}</a>");
        var breadcrumb = $"<nav class=\"breadcrumbs\" aria-label=\"Breadcrumb\">{string.Join(separatorIcon, breadcrumbItems)}</nav>";
        return renderedMarkdown[..headingStart] + breadcrumb + Environment.NewLine + renderedMarkdown[headingStart..];
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
