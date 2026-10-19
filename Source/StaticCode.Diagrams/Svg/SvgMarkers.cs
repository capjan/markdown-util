using System.Text;
using StaticCode.Diagrams.Layout;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Svg;

internal enum MarkerKind
{
    None,
    Arrow,
    OpenArrow,

    /// <summary>A hollow triangle, as for UML generalization and realization.</summary>
    Triangle,
    Diamond,
    HollowDiamond,
    Circle,
    Cross
}

/// <summary>Line end markers drawn as plain shapes, so they need no ids and follow the theme through CSS classes.</summary>
internal static class SvgMarkers
{
    private const double ArrowLength = 9;
    private const double ArrowWidth = 8;
    private const double TriangleLength = 12;
    private const double TriangleWidth = 11;
    private const double DiamondLength = 14;
    private const double DiamondWidth = 10;

    /// <summary>How far a line stops before the marker tip so that it does not poke through the marker.</summary>
    public static double Inset(MarkerKind kind, double scale = 1) => kind switch
    {
        MarkerKind.Arrow => ArrowLength * scale * 0.7,
        MarkerKind.Triangle => TriangleLength * scale,
        MarkerKind.Diamond or MarkerKind.HollowDiamond => DiamondLength * scale,
        MarkerKind.OpenArrow or MarkerKind.Cross => 1,
        MarkerKind.Circle => 8,
        _ => 0
    };

    /// <param name="direction">Unit vector pointing into the tip.</param>
    public static void Write(StringBuilder svg, MarkerKind kind, Point tip, Point direction, double scale = 1)
    {
        var normal = direction.Perpendicular;
        var back = tip - direction * (ArrowLength * scale);
        var left = back + normal * (ArrowWidth * scale / 2);
        var right = back - normal * (ArrowWidth * scale / 2);
        switch (kind)
        {
            case MarkerKind.Arrow:
            {
                var notch = tip - direction * (ArrowLength * scale * 0.7);
                svg.Append($"<path class=\"diagram-marker\" d=\"M{F(tip.X)} {F(tip.Y)} L{F(left.X)} {F(left.Y)} L{F(notch.X)} {F(notch.Y)} L{F(right.X)} {F(right.Y)} Z\"/>");
                break;
            }
            case MarkerKind.Triangle:
            {
                var triangleBack = tip - direction * (TriangleLength * scale);
                var triangleLeft = triangleBack + normal * (TriangleWidth * scale / 2);
                var triangleRight = triangleBack - normal * (TriangleWidth * scale / 2);
                svg.Append($"<path class=\"diagram-marker-hollow\" d=\"M{F(tip.X)} {F(tip.Y)} L{F(triangleLeft.X)} {F(triangleLeft.Y)} L{F(triangleRight.X)} {F(triangleRight.Y)} Z\"/>");
                break;
            }
            case MarkerKind.Diamond:
            case MarkerKind.HollowDiamond:
            {
                var middle = tip - direction * (DiamondLength * scale / 2);
                var diamondBack = tip - direction * (DiamondLength * scale);
                var diamondLeft = middle + normal * (DiamondWidth * scale / 2);
                var diamondRight = middle - normal * (DiamondWidth * scale / 2);
                var cssClass = kind == MarkerKind.Diamond ? "diagram-marker" : "diagram-marker-hollow";
                svg.Append($"<path class=\"{cssClass}\" d=\"M{F(tip.X)} {F(tip.Y)} L{F(diamondLeft.X)} {F(diamondLeft.Y)} L{F(diamondBack.X)} {F(diamondBack.Y)} L{F(diamondRight.X)} {F(diamondRight.Y)} Z\"/>");
                break;
            }
            case MarkerKind.OpenArrow:
                svg.Append($"<path class=\"diagram-marker-open\" d=\"M{F(left.X)} {F(left.Y)} L{F(tip.X)} {F(tip.Y)} L{F(right.X)} {F(right.Y)}\"/>");
                break;
            case MarkerKind.Circle:
            {
                var center = tip - direction * 4;
                svg.Append($"<circle class=\"diagram-marker\" cx=\"{F(center.X)}\" cy=\"{F(center.Y)}\" r=\"4\"/>");
                break;
            }
            case MarkerKind.Cross:
            {
                var center = tip - direction * 6;
                var a = (direction + normal) * 3.2;
                var b = (direction - normal) * 3.2;
                svg.Append($"<path class=\"diagram-marker-cross\" d=\"M{F((center - a).X)} {F((center - a).Y)} L{F((center + a).X)} {F((center + a).Y)} M{F((center - b).X)} {F((center - b).Y)} L{F((center + b).X)} {F((center + b).Y)}\"/>");
                break;
            }
        }
    }
}
