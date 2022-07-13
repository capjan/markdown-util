using System.Reflection;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.SyntaxHighlighting;
using MarkdownRenderer.Res.Model;
using Microsoft.CodeAnalysis;
using RazorLight;

namespace MarkdownRenderer;

public class HtmlRenderer
{
    private readonly RazorLightEngine _engine;
     

    public HtmlRenderer()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var metadataReference = MetadataReference.CreateFromFile(assemblyPath);
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(HtmlRenderer).Assembly)
            .AddMetadataReferences(metadataReference)
            .UseMemoryCachingProvider()
       
            .Build();
    }
    public void PrepareBasePath(string basePath)
    {
        var cssFolder = Path.Combine(basePath, "css");
        Directory.CreateDirectory(cssFolder);
        var mainCssPath = Path.Combine(cssFolder, "main.css");
        using var fs = new FileStream(mainCssPath, FileMode.Create, FileAccess.Write);
        using var mainCssStream = Resources.MainCssStream();
        mainCssStream.CopyTo(fs);
    }
    
    public async Task WriteHeadAndBody(TextWriter writer, string title, string description, int nestingDeep, string markdown)
    {
        var prefix = string.Concat(Enumerable.Repeat("../", nestingDeep));
        var cssFileLink = prefix + "css/main.css";
        
        var pattern = @"(?<prefix>\[[^\[]+\]\()(?<link>\.{1,2}/[^)]+\.md)\)";
        var replacedMarkdown = Regex.Replace(markdown, pattern, LinkReplacement);

        var pipeline = new MarkdownPipelineBuilder()
            .UseGridTables()
            .UsePipeTables()
            .UseAdvancedExtensions()
            .UseSyntaxHighlighting()
            .Build();
        
        var renderedMarkdown = Markdown.ToHtml(replacedMarkdown, pipeline);
        var model = new MainModel
        {
            Title = title,
            Description = description,
            RenderedMarkdown = renderedMarkdown,
            CssLink = cssFileLink
        };
        
        var renderedHtml = await _engine.CompileRenderAsync("MarkdownRenderer.Res.Html.main.cshtml", model);
        
        await writer.WriteAsync(renderedHtml);
    }

    string LinkReplacement(Match m)
    {
        var link = m.Groups["link"].Value;
        link = link.Substring(0, link.Length - 3);
        link += ".html";
        var prefix = m.Groups["prefix"].Value;
        
        return $"{prefix}{link})";
    }
    
    // public void WriteAfterBody(TextWriter writer)
    // {
    //     writer.WriteLine("</body>");
    //     writer.WriteLine("</html>");
    // }
}