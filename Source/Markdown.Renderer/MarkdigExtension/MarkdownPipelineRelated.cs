using Markdig;
using Markdig.Extensions.Alerts;

namespace Markdown.Renderer.MarkdigExtension;

public static class MarkdownPipelineRelated
{
    public static MarkdownPipelineBuilder UseStaticCodeHighlighter(
        this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.Add(new HighlighterExtension());
        return pipeline;
    }

    /// <summary>
    /// Renders GitHub alerts (<c>[!NOTE]</c>, <c>[!TIP]</c>, <c>[!IMPORTANT]</c>, <c>[!WARNING]</c>, <c>[!CAUTION]</c>)
    /// with Markdig's alert blocks, following GitHub's rules.
    /// </summary>
    public static MarkdownPipelineBuilder UseGitHubAlerts(
        this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<AlertExtension>();
        pipeline.Extensions.AddIfNotAlready<GitHubAlertExtension>();
        return pipeline;
    }
    
}
