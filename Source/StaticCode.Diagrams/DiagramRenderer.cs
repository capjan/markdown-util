using System.Diagnostics.CodeAnalysis;
using StaticCode.Diagrams.Context;

namespace StaticCode.Diagrams;

/// <summary>
/// Renders fenced code blocks that hold diagrams: <c>mermaid</c> for the supported Mermaid subset and
/// <c>context</c> for context diagrams.
/// </summary>
public static class DiagramRenderer
{
    /// <exception cref="DiagramSyntaxException">The source is not a supported context diagram.</exception>
    public static string RenderContextSvg(string source) => ContextSvgWriter.Write(ContextParser.Parse(source));

    /// <summary>Returns <c>false</c> for other languages and for diagrams outside the supported syntax.</summary>
    public static bool TryRenderSvg(string language, string source, [NotNullWhen(true)] out string? svg)
    {
        try
        {
            svg = language.ToLowerInvariant() switch
            {
                "mermaid" => MermaidRenderer.RenderSvg(source),
                "context" => RenderContextSvg(source),
                _ => null
            };
        }
        catch (DiagramSyntaxException)
        {
            svg = null;
        }

        return svg != null;
    }
}
