using Markdig;
using Markdown.Renderer.MarkdigExtension;

namespace Markdown.Renderer.Test;

public class MathTest
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseRenderedMath()
        .UseStaticCodeHighlighter()
        .Build();

    private static string Render(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline).ReplaceLineEndings("\n");

    [Fact]
    public void RendersInlineMathAsStaticSvg()
    {
        var html = Render("Value $x^2$.");

        Assert.Contains("<span class=\"math-inline\" role=\"math\" aria-label=\"x^2\"><svg", html);
        Assert.Contains("class=\"math-svg\"", html);
        Assert.Contains("var(--text-primary)", html);
        Assert.DoesNotContain("\\(x^2\\)", html);
    }

    [Fact]
    public void RendersDisplayAndFencedMathAsStaticSvg()
    {
        var displayHtml = Render("$$\n\\frac{1}{2}\n$$\n");
        var fencedHtml = Render("```math\n\\sqrt{x}\n```\n");

        Assert.Contains("<div class=\"math-display\" role=\"math\"", displayHtml);
        Assert.Contains("<svg", displayHtml);
        Assert.Contains("<div class=\"math-display\" role=\"math\"", fencedHtml);
        Assert.Contains("<svg", fencedHtml);
        Assert.DoesNotContain("<pre", fencedHtml);
    }

    [Fact]
    public void KeepsUnsupportedMathVisibleAndEncoded()
    {
        var html = Render("```math\n\\notarealcommand{<script>}\n```\n");

        Assert.Contains("<pre class=\"math-source\"><code>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }
}
