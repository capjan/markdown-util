using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdown.Renderer.MarkdigExtension;
using Markdown.Renderer.Res.Model;
using Microsoft.CodeAnalysis;
using RazorLight;

namespace Markdown.Renderer;

public class HtmlRenderer
{
    private readonly RazorLightEngine _engine;
    private readonly MarkdownPipeline _pipeline;
    private readonly LinkReplacer _linkReplacer = new LinkReplacer();

    public HtmlRenderer()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var metadataReference = MetadataReference.CreateFromFile(assemblyPath);
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(HtmlRenderer).Assembly)
            .AddMetadataReferences(metadataReference)
            .UseMemoryCachingProvider()
       
            .Build();
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
        string editPageLink)
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
            CssLink = cssFileLink,
            EditPageLink = editPageLink
        };
        
        var renderedHtml = await _engine.CompileRenderAsync("Markdown.Renderer.Res.Html.main.cshtml", model);
        
        await writer.WriteAsync(renderedHtml);
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