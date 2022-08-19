using Markdig;

namespace Markdown.Renderer.MarkdigExtension;

public static class MarkdownPipelineRelated
{
    public static MarkdownPipelineBuilder UseStaticCodeHighlighter(
        this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.Add(new HighlighterExtension());
        return pipeline;
    }
    
}