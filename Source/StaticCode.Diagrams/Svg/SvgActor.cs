using System.Text;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Text;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Svg;

/// <summary>The stick figure for people, shared by every diagram type, with its label below.</summary>
internal static class SvgActor
{
    public const double FigureHeight = 38;
    private const double LabelGap = 4;

    public static (double Width, double Height) Size(IReadOnlyList<string> lines)
    {
        var (textWidth, textHeight) = TextMetrics.Measure(lines);
        return (Math.Max(textWidth + 8, 48), FigureHeight + LabelGap + textHeight);
    }

    /// <summary>Writes the figure and its label, with the figure's head at <paramref name="top"/>.</summary>
    public static void Write(StringBuilder svg, IReadOnlyList<string> lines, double x, double top)
    {
        svg.Append($"<circle class=\"diagram-actor-head\" cx=\"{F(x)}\" cy=\"{F(top + 7)}\" r=\"7\"/>")
            .Append($"<path class=\"diagram-actor-body\" d=\"M{F(x)} {F(top + 14)} V{F(top + 27)} M{F(x - 11)} {F(top + 19)} H{F(x + 11)}")
            .Append($" M{F(x)} {F(top + 27)} L{F(x - 9)} {F(top + 37)} M{F(x)} {F(top + 27)} L{F(x + 9)} {F(top + 37)}\"/>");
        var textHeight = lines.Count * TextMetrics.LineHeight;
        WriteText(svg, lines, new Point(x, top + FigureHeight + LabelGap + textHeight / 2), "diagram-node-text");
    }
}
