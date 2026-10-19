using System.Globalization;
using System.Text.Encodings.Web;
using CSharpMath.VectSharp;
using Markdig;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using VectSharp;
using VectSharp.SVG;

namespace Markdown.Renderer.MarkdigExtension;

public sealed class RenderedMathExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not Markdig.Renderers.HtmlRenderer htmlRenderer) return;

        var mathBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<HtmlMathBlockRenderer>();
        if (mathBlockRenderer is not null) htmlRenderer.ObjectRenderers.Remove(mathBlockRenderer);
        htmlRenderer.ObjectRenderers.AddIfNotAlready(new RenderedMathBlockRenderer());

        var mathInlineRenderer = htmlRenderer.ObjectRenderers.FindExact<HtmlMathInlineRenderer>();
        if (mathInlineRenderer is not null) htmlRenderer.ObjectRenderers.Remove(mathInlineRenderer);
        htmlRenderer.ObjectRenderers.AddIfNotAlready(new RenderedMathInlineRenderer());
    }
}

internal sealed class RenderedMathBlockRenderer : HtmlObjectRenderer<MathBlock>
{
    protected override void Write(Markdig.Renderers.HtmlRenderer renderer, MathBlock block)
    {
        var latex = string.Join("\n", block.Lines.Lines?.Select(line => line.Slice.ToString()) ?? [])
            .Trim('\r', '\n');
        renderer.WriteLine(MathFormulaRenderer.Render(latex, display: true));
    }
}

internal sealed class RenderedMathInlineRenderer : HtmlObjectRenderer<MathInline>
{
    protected override void Write(Markdig.Renderers.HtmlRenderer renderer, MathInline inline) =>
        renderer.Write(MathFormulaRenderer.Render(inline.Content.ToString(), display: false));
}

internal static class MathFormulaRenderer
{
    // CSharpMath and VectSharp share non-thread-safe caches, so formulas are rendered one at a time.
    private static readonly Lock RenderLock = new();

    public static string Render(string latex, bool display)
    {
        lock (RenderLock)
        {
            return RenderUnsynchronized(latex, display);
        }
    }

    private static string RenderUnsynchronized(string latex, bool display)
    {
        try
        {
            var painter = new MathPainter
            {
                LaTeX = latex,
                FontSize = 16,
                TextColor = Colours.Black
            };
            if (!string.IsNullOrEmpty(painter.ErrorMessage)) return RenderSource(latex, display);

            var page = painter.DrawToPage();
            if (!string.IsNullOrEmpty(painter.ErrorMessage)) return RenderSource(latex, display);

            var svg = SVGContextInterpreter.SaveAsSVG(
                page,
                SVGContextInterpreter.TextOptions.ConvertIntoPathsUsingGlyphs);
            var root = svg.DocumentElement;
            if (root is null) return RenderSource(latex, display);

            root.SetAttribute("class", "math-svg");
            root.SetAttribute("width", page.Width.ToString("0.##", CultureInfo.InvariantCulture));
            root.SetAttribute("height", page.Height.ToString("0.##", CultureInfo.InvariantCulture));
            root.SetAttribute("aria-hidden", "true");
            root.SetAttribute("focusable", "false");

            foreach (System.Xml.XmlElement element in root.GetElementsByTagName("*"))
            {
                if (element.GetAttribute("fill") == "#000000")
                    element.SetAttribute("fill", "var(--text-primary)");
                if (element.GetAttribute("stroke") == "#000000")
                    element.SetAttribute("stroke", "var(--text-primary)");
            }

            var formula = HtmlEncoder.Default.Encode(latex);
            var renderedSvg = root.OuterXml;
            return display
                ? $"<div class=\"math-display\" role=\"math\" aria-label=\"{formula}\">{renderedSvg}</div>"
                : $"<span class=\"math-inline\" role=\"math\" aria-label=\"{formula}\">{renderedSvg}</span>";
        }
        catch (Exception)
        {
            return RenderSource(latex, display);
        }
    }

    private static string RenderSource(string latex, bool display)
    {
        var encodedLatex = HtmlEncoder.Default.Encode(latex);
        return display
            ? $"<pre class=\"math-source\"><code>{encodedLatex}</code></pre>"
            : $"<code class=\"math-source\">{encodedLatex}</code>";
    }
}
