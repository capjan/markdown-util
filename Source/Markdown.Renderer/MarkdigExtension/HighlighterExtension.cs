using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Markdown.Renderer.MarkdigExtension;

/// <summary>
/// Extension for Markdig to provide rendering a Code Block with StaticCode.Highlighter
/// </summary>
public class HighlighterExtension : IMarkdownExtension
{

    public void Setup(MarkdownPipelineBuilder pipeline)
    {

    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof (renderer));
        if (!(renderer is TextRendererBase<Markdig.Renderers.HtmlRenderer> textRendererBase))
            return;
        var exact = textRendererBase.ObjectRenderers.FindExact<CodeBlockRenderer>();
        if (exact != null)
            textRendererBase.ObjectRenderers.Remove(exact);
        textRendererBase.ObjectRenderers.AddIfNotAlready(new HighlightedCodeBlockRenderer());
    }
}