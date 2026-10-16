using System.Diagnostics.CodeAnalysis;
using StaticCode.Diagrams.Flowchart;

namespace StaticCode.Diagrams;

/// <summary>
/// Renders a supported subset of Mermaid diagram syntax to static, themeable SVG without JavaScript.
/// </summary>
public static class MermaidRenderer
{
    /// <exception cref="DiagramSyntaxException">The source is not a supported diagram.</exception>
    public static string RenderSvg(string source)
    {
        if (!FlowchartParser.CanParse(source))
            throw new DiagramSyntaxException("Only flowchart diagrams are supported.", 1);
        return FlowchartSvgWriter.Write(FlowchartParser.Parse(source));
    }

    public static bool TryRenderSvg(string source, [NotNullWhen(true)] out string? svg)
    {
        try
        {
            svg = RenderSvg(source);
            return true;
        }
        catch (DiagramSyntaxException)
        {
            svg = null;
            return false;
        }
    }
}
