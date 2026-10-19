using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;

namespace Markdown.Renderer.MarkdigExtension;

public sealed class ScrollableTableExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not Markdig.Renderers.HtmlRenderer htmlRenderer) return;

        var tableRenderer = htmlRenderer.ObjectRenderers.FindExact<HtmlTableRenderer>();
        if (tableRenderer is null) return;

        htmlRenderer.ObjectRenderers.Remove(tableRenderer);
        htmlRenderer.ObjectRenderers.Add(new ScrollableTableRenderer());
    }
}

internal sealed class ScrollableTableRenderer : HtmlTableRenderer
{
    protected override void Write(Markdig.Renderers.HtmlRenderer renderer, Table table)
    {
        renderer.WriteLine("<div class=\"table-wrapper\">");
        base.Write(renderer, table);
        renderer.WriteLine("</div>");
    }
}
