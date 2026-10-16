using System.Diagnostics.CodeAnalysis;
using StaticCode.Diagrams.Flowchart;
using StaticCode.Diagrams.Sequence;

namespace StaticCode.Diagrams;

/// <summary>
/// Renders a supported subset of Mermaid diagram syntax to static, themeable SVG without JavaScript.
/// </summary>
public static class MermaidRenderer
{
    /// <exception cref="DiagramSyntaxException">The source is not a supported diagram.</exception>
    public static string RenderSvg(string source)
    {
        if (FlowchartParser.CanParse(source)) return FlowchartSvgWriter.Write(FlowchartParser.Parse(source));
        if (SequenceParser.CanParse(source)) return SequenceSvgWriter.Write(SequenceParser.Parse(source));
        throw new DiagramSyntaxException("Only flowcharts and sequence diagrams are supported.", 1);
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
