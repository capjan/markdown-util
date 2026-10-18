using System.Text;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Svg;
using StaticCode.Diagrams.Text;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Flowchart;

internal sealed record NodeBox(NodeShape Shape, IReadOnlyList<string> Lines, double Width, double Height, double CornerRadius);

internal sealed record LabelBox(IReadOnlyList<string> Lines, double Width, double Height);

/// <summary>Sizes, outlines, and drawing of flowchart nodes and links, shared by every diagram built from them.</summary>
internal static class NodeShapes
{
    private const double NodePaddingX = 16;
    private const double NodePaddingY = 10;
    private const double MinNodeWidth = 56;
    private const double MaxNodeTextWidth = 200;
    private const double MaxEdgeTextWidth = 160;
    private const double LabelPaddingX = 6;
    private const double LabelPaddingY = 2;

    public static NodeBox CreateNode(FlowchartNode node)
    {
        var lines = TextMetrics.Wrap(node.Label, MaxNodeTextWidth);
        var (textWidth, textHeight) = TextMetrics.Measure(lines);
        switch (node.Shape)
        {
            case NodeShape.Actor:
            {
                var (width, height) = SvgActor.Size(lines);
                return new NodeBox(node.Shape, lines, width, height, 0);
            }
            case NodeShape.Stadium:
            {
                var height = textHeight + 2 * NodePaddingY;
                return new NodeBox(node.Shape, lines, Math.Max(textWidth + height + 8, MinNodeWidth), height, height / 2);
            }
            case NodeShape.Circle:
            {
                var halfWidth = textWidth / 2 + 10;
                var halfHeight = textHeight / 2 + 6;
                var diameter = Math.Max(2 * Math.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight), 40);
                return new NodeBox(node.Shape, lines, diameter, diameter, diameter / 2);
            }
            case NodeShape.Diamond:
            {
                // A diamond with half diagonals a and b contains the text box (p, q) when p/a + q/b <= 1.
                // a = p + q·c and b = q + p/c satisfy this for any c; a larger c gives a flatter diamond.
                var p = textWidth / 2 + 12;
                var q = textHeight / 2 + 6;
                var c = Math.Clamp(p / q / 2, 1, 2.5);
                return new NodeBox(node.Shape, lines, 2 * (p + q * c), 2 * (q + p / c), 0);
            }
            default:
            {
                var height = textHeight + 2 * NodePaddingY;
                var radius = node.Shape == NodeShape.Rounded ? Math.Min(12, height / 2) : 4;
                return new NodeBox(node.Shape, lines, Math.Max(textWidth + 2 * NodePaddingX, MinNodeWidth), height, radius);
            }
        }
    }

    public static LabelBox CreateLabel(string text)
    {
        var lines = TextMetrics.Wrap(text, MaxEdgeTextWidth);
        var (width, height) = TextMetrics.Measure(lines);
        return new LabelBox(lines, width + 2 * LabelPaddingX, height + 2 * LabelPaddingY);
    }

    /// <summary>Distance from the center to the outline in one direction, at an offset in the perpendicular direction.</summary>
    public static double Extent(NodeBox box, double halfAlong, double halfAcross, double offset)
    {
        var distance = Math.Min(Math.Abs(offset), halfAcross);
        switch (box.Shape)
        {
            case NodeShape.Circle:
                return Math.Sqrt(Math.Max(0, halfAlong * halfAlong - distance * distance));
            case NodeShape.Diamond:
                return halfAlong * (1 - distance / halfAcross);
            default:
                var radius = Math.Min(box.CornerRadius, Math.Min(halfAlong, halfAcross));
                if (distance <= halfAcross - radius) return halfAlong;
                var inset = distance - (halfAcross - radius);
                return halfAlong - radius + Math.Sqrt(Math.Max(0, radius * radius - inset * inset));
        }
    }

    /// <summary>Distance from the center to where a ray in <paramref name="direction"/> leaves the outline.</summary>
    public static double Boundary(NodeBox box, Point direction)
    {
        var u = direction.Normalized;
        var halfWidth = box.Width / 2;
        var halfHeight = box.Height / 2;
        var ux = Math.Abs(u.X);
        var uy = Math.Abs(u.Y);
        switch (box.Shape)
        {
            case NodeShape.Circle:
                return halfWidth;
            case NodeShape.Diamond:
                return 1 / (ux / halfWidth + uy / halfHeight);
        }

        var distance = Math.Min(ux > 0 ? halfWidth / ux : double.PositiveInfinity, uy > 0 ? halfHeight / uy : double.PositiveInfinity);
        var radius = Math.Min(box.CornerRadius, Math.Min(halfWidth, halfHeight));
        var cornerX = halfWidth - radius;
        var cornerY = halfHeight - radius;
        if (radius <= 0 || ux * distance <= cornerX || uy * distance <= cornerY) return distance;

        // The ray leaves through a rounded corner: intersect it with the corner circle and take the far root.
        var b = ux * cornerX + uy * cornerY;
        var c = cornerX * cornerX + cornerY * cornerY - radius * radius;
        return b + Math.Sqrt(Math.Max(0, b * b - c));
    }

    /// <summary>How far the shape reaches in <paramref name="direction"/> (its support function); used to keep shapes apart.</summary>
    public static double Reach(NodeBox box, Point direction)
    {
        var u = direction.Normalized;
        var x = Math.Abs(u.X) * box.Width / 2;
        var y = Math.Abs(u.Y) * box.Height / 2;
        return box.Shape switch
        {
            NodeShape.Circle => box.Width / 2,
            NodeShape.Diamond => Math.Max(x, y),
            _ => x + y
        };
    }

    public static void WriteNode(StringBuilder svg, NodeBox box, Point center, string? extraClass = null, string? href = null)
    {
        var classes = $"diagram-node diagram-node-{box.Shape.ToString().ToLowerInvariant()}" + (extraClass == null ? string.Empty : $" {extraClass}");
        var safeHref = SafeHref(href);
        var pdfBounds = $"{F(center.X - box.Width / 2)} {F(center.Y - box.Height / 2)} {F(box.Width)} {F(box.Height)}";
        if (safeHref != null) svg.Append($"<a class=\"diagram-link\" href=\"{safeHref}\" data-pdf-bounds=\"{pdfBounds}\">");
        svg.Append($"<g class=\"{classes}\">");
        var left = center.X - box.Width / 2;
        var top = center.Y - box.Height / 2;
        switch (box.Shape)
        {
            case NodeShape.Actor:
                SvgActor.Write(svg, box.Lines, center.X, top);
                svg.Append("</g>");
                if (safeHref != null) svg.Append("</a>");
                svg.Append('\n');
                return;
            case NodeShape.Circle:
                svg.Append($"<circle class=\"diagram-node-shape\" cx=\"{F(center.X)}\" cy=\"{F(center.Y)}\" r=\"{F(box.Width / 2)}\"/>");
                break;
            case NodeShape.Diamond:
                svg.Append($"<polygon class=\"diagram-node-shape\" points=\"{F(center.X)},{F(top)} {F(left + box.Width)},{F(center.Y)} {F(center.X)},{F(top + box.Height)} {F(left)},{F(center.Y)}\"/>");
                break;
            default:
                svg.Append($"<rect class=\"diagram-node-shape\" x=\"{F(left)}\" y=\"{F(top)}\" width=\"{F(box.Width)}\" height=\"{F(box.Height)}\" rx=\"{F(box.CornerRadius)}\"/>");
                break;
        }

        WriteText(svg, box.Lines, center, "diagram-node-text");
        svg.Append("</g>");
        if (safeHref != null) svg.Append("</a>");
        svg.Append('\n');
    }

    public static void WriteLabel(StringBuilder svg, LabelBox label, Point center)
    {
        svg.Append("<g class=\"diagram-edge-label\">")
            .Append($"<rect class=\"diagram-edge-label-background\" x=\"{F(center.X - label.Width / 2)}\" y=\"{F(center.Y - label.Height / 2)}\"")
            .Append($" width=\"{F(label.Width)}\" height=\"{F(label.Height)}\" rx=\"4\"/>");
        WriteText(svg, label.Lines, center, "diagram-edge-label-text");
        svg.Append("</g>\n");
    }

    public static string EdgeClass(LineStyle style) => style switch
    {
        LineStyle.Dotted => "diagram-edge diagram-edge-dotted",
        LineStyle.Thick => "diagram-edge diagram-edge-thick",
        _ => "diagram-edge"
    };

    public static MarkerKind Marker(EdgeMarker marker) => marker switch
    {
        EdgeMarker.Arrow => MarkerKind.Arrow,
        EdgeMarker.Circle => MarkerKind.Circle,
        EdgeMarker.Cross => MarkerKind.Cross,
        _ => MarkerKind.None
    };

    public static double MarkerScale(LineStyle style) => style == LineStyle.Thick ? 1.3 : 1;
}
