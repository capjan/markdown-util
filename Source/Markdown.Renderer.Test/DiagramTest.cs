using Markdig;
using Markdown.Renderer.MarkdigExtension;

namespace Markdown.Renderer.Test;

public class DiagramTest
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseStaticCodeHighlighter()
        .Build();

    private static string Render(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline).ReplaceLineEndings("\n");

    [Fact]
    public void RendersMermaidFlowchartsAsInlineSvg()
    {
        var html = Render("```mermaid\nflowchart LR\n    A --> B\n```\n");

        Assert.StartsWith("<figure class=\"diagram\"><svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-flowchart\"", html);
        Assert.EndsWith("</svg></figure>\n", html);
        Assert.DoesNotContain("<pre", html);
        Assert.DoesNotContain("class=\"mermaid\"", html);
    }

    [Fact]
    public void RendersMermaidSequenceDiagramsAsInlineSvg()
    {
        var html = Render("```mermaid\nsequenceDiagram\n    A->>B: Hello\n```\n");

        Assert.StartsWith("<figure class=\"diagram\"><svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-sequence\"", html);
        Assert.DoesNotContain("<pre", html);
    }

    [Fact]
    public void RendersContextDiagramsAsInlineSvg()
    {
        var html = Render("```context\nApp[App]\nactor User\nUser --> App\n```\n");

        Assert.StartsWith("<figure class=\"diagram\"><svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-context\"", html);
        Assert.DoesNotContain("<pre", html);
    }

    [Fact]
    public void RendersPlantUmlComponentDiagramsAsInlineSvg()
    {
        var html = Render("```plantuml\n@startuml\n[App] --> [Database]\n@enduml\n```\n");

        Assert.StartsWith("<figure class=\"diagram\"><svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-component\"", html);
        Assert.DoesNotContain("<pre", html);
    }

    [Fact]
    public void KeepsUnsupportedMermaidDiagramsAsCodeBlocks()
    {
        var html = Render("```mermaid\npie\n    \\\"Dogs\\\" : 1\n```\n");

        Assert.Contains("<pre", html);
        Assert.Contains("pie", html);
        Assert.DoesNotContain("<figure class=\"diagram\"", html);
    }
}
