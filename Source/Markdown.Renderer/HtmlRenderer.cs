using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Markdig;
using Markdown.Renderer.MarkdigExtension;
using Markdown.Renderer.Res.Model;

namespace Markdown.Renderer;

public class HtmlRenderer
{
    private static readonly Regex TemplateTokenRegex = new(@"%%(?<name>[A-Z_]+)%%", RegexOptions.Compiled);
    private readonly MarkdownPipeline _pipeline;
    private readonly LinkReplacer _linkReplacer = new LinkReplacer();

    public HtmlRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseGridTables()
            .UsePipeTables()
            .UseAdvancedExtensions()
            .UseStaticCodeHighlighter()
            .Build();
    }
    public void PrepareBasePath(string basePath)
    {
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
        IReadOnlyList<NavigationItem>? navigationItems = null)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", nestingDeep));
        var cssFileLink = prefix + "css/main.css";
        
        var replacedMarkdown = _linkReplacer.ReplaceAll(markdown);
        
        var renderedMarkdown = Markdig.Markdown.ToHtml(replacedMarkdown, _pipeline);
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
            ["NAVIGATION_ITEMS"] = RenderNavigationItems(model.NavigationItems),
            ["MARKDOWN"] = model.RenderedMarkdown,
            ["EDIT_PAGE_LINK"] = RenderEditPageLink(model.EditPageLink)
        };
        var renderedHtml = TemplateTokenRegex.Replace(Resources.HtmlTemplateContent(), match =>
            values.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value);
        
        await writer.WriteAsync(renderedHtml);
    }

    private static string RenderNavigationItems(IReadOnlyList<NavigationItem> navigationItems) =>
        string.Join(Environment.NewLine, navigationItems.Select(item => $$"""
            <li class="site-nav-item" style="--depth: {{item.Depth}}">
                <a class="site-nav-link {{(item.IsCurrent ? "is-current" : string.Empty)}}" href="{{HtmlEncoder.Default.Encode(item.Url)}}" aria-current="{{(item.IsCurrent ? "page" : "false")}}">{{HtmlEncoder.Default.Encode(item.Title)}}</a>
            </li>
            """));

    private static string RenderEditPageLink(string editPageLink) =>
        string.IsNullOrEmpty(editPageLink)
            ? string.Empty
            : $$"""
              <hr />
              <a href="{{HtmlEncoder.Default.Encode(editPageLink)}}">✎ Edit Page</a>
              """;
    
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
