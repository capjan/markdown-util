using System.Globalization;
using System.Text;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Text;

namespace StaticCode.Diagrams.Flowchart;

/// <summary>
/// Writes a laid-out flowchart as inline SVG. The SVG contains no colors: every element carries a
/// <c>diagram-*</c> class so the page stylesheet can theme it for light and dark mode.
/// </summary>
internal static class FlowchartSvgWriter
{
    private const double NodePaddingX = 16;
    private const double NodePaddingY = 10;
    private const double MinNodeWidth = 56;
    private const double MaxNodeTextWidth = 200;
    private const double MaxEdgeTextWidth = 160;
    private const double LabelPaddingX = 6;
    private const double LabelPaddingY = 2;
    private const double ArrowLength = 9;
    private const double ArrowWidth = 8;
    private const double LoopSize = 22;
    private const double CanvasPadding = 4;

    public static string Write(FlowchartDiagram diagram)
    {
        var horizontal = diagram.Direction is FlowDirection.LeftToRight or FlowDirection.RightToLeft;
        var axis = horizontal ? new Point(1, 0) : new Point(0, 1);
        var cross = horizontal ? new Point(0, 1) : new Point(1, 0);

        var nodeIndex = diagram.Nodes.Select((node, index) => (node.Id, index)).ToDictionary(item => item.Id, item => item.index);
        var boxes = diagram.Nodes.Select(CreateNodeBox).ToList();
        var labels = diagram.Edges.Select(edge => edge.Label == null ? null : CreateLabelBox(edge.Label)).ToList();

        var loopSizes = new double[diagram.Edges.Count];
        var margins = new double[boxes.Count];
        var loopCounts = new int[boxes.Count];
        for (var index = 0; index < diagram.Edges.Count; index++)
        {
            var edge = diagram.Edges[index];
            if (edge.From != edge.To) continue;
            var node = nodeIndex[edge.From];
            loopSizes[index] = LoopSize + 10 * loopCounts[node]++;
            var label = labels[index];
            var labelExtent = label == null ? 0 : 6 + (horizontal ? label.Height : label.Width);
            margins[node] = Math.Max(margins[node], loopSizes[index] * 1.1 + labelExtent + 4);
        }

        var layout = new LayeredLayout().Compute(
            boxes.Select((box, index) => new LayoutNode(box.Width, box.Height, margins[index])).ToList(),
            diagram.Edges.Select((edge, index) => new LayoutEdge(
                nodeIndex[edge.From],
                nodeIndex[edge.To],
                edge.Length,
                labels[index]?.Width ?? 0,
                labels[index]?.Height ?? 0)).ToList(),
            diagram.Direction switch
            {
                FlowDirection.BottomToTop => LayoutDirection.BottomToTop,
                FlowDirection.LeftToRight => LayoutDirection.LeftToRight,
                FlowDirection.RightToLeft => LayoutDirection.RightToLeft,
                _ => LayoutDirection.TopToBottom
            });

        var width = layout.Width + 2 * CanvasPadding;
        var height = layout.Height + 2 * CanvasPadding;
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-flowchart\"")
            .Append($" viewBox=\"{F(-CanvasPadding)} {F(-CanvasPadding)} {F(width)} {F(height)}\"")
            .Append($" width=\"{F(width)}\" height=\"{F(height)}\"")
            .Append($" font-size=\"{F(TextMetrics.FontSize)}\" role=\"img\" aria-label=\"{Escape(diagram.Title ?? "Flowchart")}\">\n");

        var labelCenters = new Point?[diagram.Edges.Count];
        svg.Append("<g class=\"diagram-edges\">\n");
        for (var index = 0; index < diagram.Edges.Count; index++)
        {
            var edge = diagram.Edges[index];
            var from = nodeIndex[edge.From];
            var to = nodeIndex[edge.To];
            if (from == to)
            {
                labelCenters[index] = WriteSelfLoop(svg, edge, boxes[from], layout.NodeCenters[from], loopSizes[index], labels[index], axis, cross, horizontal);
                continue;
            }

            var points = layout.Edges[index].Points.ToList();
            points[0] = SnapToShape(boxes[from], layout.NodeCenters[from], points[0], axis, cross, horizontal);
            points[^1] = SnapToShape(boxes[to], layout.NodeCenters[to], points[^1], axis, cross, horizontal);
            WriteRoute(svg, edge, points, axis);
            labelCenters[index] = layout.Edges[index].LabelCenter;
        }

        svg.Append("</g>\n<g class=\"diagram-edge-labels\">\n");
        for (var index = 0; index < diagram.Edges.Count; index++)
        {
            var label = labels[index];
            if (label == null || labelCenters[index] is not { } center) continue;
            svg.Append("<g class=\"diagram-edge-label\">")
                .Append($"<rect class=\"diagram-edge-label-background\" x=\"{F(center.X - label.Width / 2)}\" y=\"{F(center.Y - label.Height / 2)}\"")
                .Append($" width=\"{F(label.Width)}\" height=\"{F(label.Height)}\" rx=\"4\"/>");
            WriteText(svg, label.Lines, center, "diagram-edge-label-text");
            svg.Append("</g>\n");
        }

        svg.Append("</g>\n<g class=\"diagram-nodes\">\n");
        for (var index = 0; index < boxes.Count; index++)
            WriteNode(svg, boxes[index], layout.NodeCenters[index]);
        svg.Append("</g>\n</svg>");
        return svg.ToString();
    }

    private static NodeBox CreateNodeBox(FlowchartNode node)
    {
        var lines = TextMetrics.Wrap(node.Label, MaxNodeTextWidth);
        var (textWidth, textHeight) = TextMetrics.Measure(lines);
        switch (node.Shape)
        {
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

    private static LabelBox CreateLabelBox(string text)
    {
        var lines = TextMetrics.Wrap(text, MaxEdgeTextWidth);
        var (width, height) = TextMetrics.Measure(lines);
        return new LabelBox(lines, width + 2 * LabelPaddingX, height + 2 * LabelPaddingY);
    }

    /// <summary>Moves an edge end from the node's bounding box onto its actual outline.</summary>
    private static Point SnapToShape(NodeBox box, Point center, Point point, Point axis, Point cross, bool horizontal)
    {
        var offset = point - center;
        var along = offset.Dot(axis);
        var across = offset.Dot(cross);
        var halfAlong = (horizontal ? box.Width : box.Height) / 2;
        var halfAcross = (horizontal ? box.Height : box.Width) / 2;
        var extent = Extent(box, halfAlong, halfAcross, across);
        return center + cross * across + axis * ((along < 0 ? -1 : 1) * extent);
    }

    /// <summary>Distance from the center to the outline in one direction, at an offset in the perpendicular direction.</summary>
    private static double Extent(NodeBox box, double halfAlong, double halfAcross, double offset)
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

    /// <summary>
    /// Draws a smooth cubic Hermite spline through the route. Interior tangents follow Catmull-Rom;
    /// the ends leave and enter nodes along the flow direction.
    /// </summary>
    private static void WriteRoute(StringBuilder svg, FlowchartEdge edge, List<Point> points, Point axis)
    {
        var startDirection = axis * Math.Sign((points[1] - points[0]).Dot(axis));
        var endDirection = axis * Math.Sign((points[^1] - points[^2]).Dot(axis));
        var start = points[0];
        var end = points[^1];
        points[0] = start + startDirection * MarkerInset(edge.StartMarker, edge.Style);
        points[^1] = end - endDirection * MarkerInset(edge.EndMarker, edge.Style);

        var count = points.Count;
        var tangents = new Point[count];
        tangents[0] = axis * ((points[1] - points[0]).Dot(axis) * 1.5);
        tangents[^1] = axis * ((points[^1] - points[^2]).Dot(axis) * 1.5);
        for (var index = 1; index < count - 1; index++)
            tangents[index] = (points[index + 1] - points[index - 1]) * 0.5;

        var path = new StringBuilder($"M{F(points[0].X)} {F(points[0].Y)}");
        for (var index = 0; index < count - 1; index++)
        {
            var c1 = points[index] + tangents[index] * (1.0 / 3);
            var c2 = points[index + 1] - tangents[index + 1] * (1.0 / 3);
            path.Append($" C{F(c1.X)} {F(c1.Y)} {F(c2.X)} {F(c2.Y)} {F(points[index + 1].X)} {F(points[index + 1].Y)}");
        }

        svg.Append($"<path class=\"{EdgeClass(edge.Style)}\" d=\"{path}\"/>");
        WriteMarker(svg, edge.EndMarker, edge.Style, end, endDirection);
        WriteMarker(svg, edge.StartMarker, edge.Style, start, startDirection * -1);
        svg.Append('\n');
    }

    private static Point? WriteSelfLoop(
        StringBuilder svg, FlowchartEdge edge, NodeBox box, Point center, double size, LabelBox? label, Point axis, Point cross, bool horizontal)
    {
        var halfAlong = (horizontal ? box.Width : box.Height) / 2;
        var halfAcross = (horizontal ? box.Height : box.Width) / 2;
        var attach = Math.Min(halfAlong * 0.45, 10);
        var side = Extent(box, halfAcross, halfAlong, attach);
        var start = center - axis * attach + cross * side;
        var end = center + axis * attach + cross * side;
        var c1 = start + cross * (size * 1.4) - axis * (size * 0.5);
        var c2 = end + cross * (size * 1.4) + axis * (size * 0.5);
        var endDirection = (end - c2).Normalized;
        var startDirection = (start - c1).Normalized;
        var pathEnd = end - endDirection * MarkerInset(edge.EndMarker, edge.Style);
        var pathStart = start - startDirection * MarkerInset(edge.StartMarker, edge.Style);

        svg.Append($"<path class=\"{EdgeClass(edge.Style)}\" d=\"M{F(pathStart.X)} {F(pathStart.Y)} C{F(c1.X)} {F(c1.Y)} {F(c2.X)} {F(c2.Y)} {F(pathEnd.X)} {F(pathEnd.Y)}\"/>");
        WriteMarker(svg, edge.EndMarker, edge.Style, end, endDirection);
        WriteMarker(svg, edge.StartMarker, edge.Style, start, startDirection);
        svg.Append('\n');

        if (label == null) return null;
        var labelExtent = horizontal ? label.Height : label.Width;
        return center + cross * (side + size * 1.1 + 6 + labelExtent / 2);
    }

    private static string EdgeClass(LineStyle style) => style switch
    {
        LineStyle.Dotted => "diagram-edge diagram-edge-dotted",
        LineStyle.Thick => "diagram-edge diagram-edge-thick",
        _ => "diagram-edge"
    };

    /// <summary>How far the line stops before the marker tip so that it does not poke through the marker.</summary>
    private static double MarkerInset(EdgeMarker marker, LineStyle style) => marker switch
    {
        EdgeMarker.Arrow => ArrowLength * ArrowScale(style) * 0.7,
        EdgeMarker.Circle => 8,
        EdgeMarker.Cross => 1,
        _ => 0
    };

    private static double ArrowScale(LineStyle style) => style == LineStyle.Thick ? 1.3 : 1;

    private static void WriteMarker(StringBuilder svg, EdgeMarker marker, LineStyle style, Point tip, Point direction)
    {
        var normal = direction.Perpendicular;
        switch (marker)
        {
            case EdgeMarker.Arrow:
            {
                var scale = ArrowScale(style);
                var back = tip - direction * (ArrowLength * scale);
                var notch = tip - direction * (ArrowLength * scale * 0.7);
                var left = back + normal * (ArrowWidth * scale / 2);
                var right = back - normal * (ArrowWidth * scale / 2);
                svg.Append($"<path class=\"diagram-marker\" d=\"M{F(tip.X)} {F(tip.Y)} L{F(left.X)} {F(left.Y)} L{F(notch.X)} {F(notch.Y)} L{F(right.X)} {F(right.Y)} Z\"/>");
                break;
            }
            case EdgeMarker.Circle:
            {
                var center = tip - direction * 4;
                svg.Append($"<circle class=\"diagram-marker\" cx=\"{F(center.X)}\" cy=\"{F(center.Y)}\" r=\"4\"/>");
                break;
            }
            case EdgeMarker.Cross:
            {
                var center = tip - direction * 6;
                var a = (direction + normal) * 3.2;
                var b = (direction - normal) * 3.2;
                svg.Append($"<path class=\"diagram-marker-cross\" d=\"M{F((center - a).X)} {F((center - a).Y)} L{F((center + a).X)} {F((center + a).Y)} M{F((center - b).X)} {F((center - b).Y)} L{F((center + b).X)} {F((center + b).Y)}\"/>");
                break;
            }
        }
    }

    private static void WriteNode(StringBuilder svg, NodeBox box, Point center)
    {
        svg.Append($"<g class=\"diagram-node diagram-node-{box.Shape.ToString().ToLowerInvariant()}\">");
        var left = center.X - box.Width / 2;
        var top = center.Y - box.Height / 2;
        switch (box.Shape)
        {
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
        svg.Append("</g>\n");
    }

    private static void WriteText(StringBuilder svg, IReadOnlyList<string> lines, Point center, string cssClass)
    {
        // Baselines are computed instead of using dominant-baseline, which renders inconsistently across browsers.
        var firstBaseline = center.Y - (lines.Count - 1) / 2.0 * TextMetrics.LineHeight + TextMetrics.FontSize * 0.35;
        if (lines.Count == 1)
        {
            svg.Append($"<text class=\"{cssClass}\" x=\"{F(center.X)}\" y=\"{F(firstBaseline)}\" text-anchor=\"middle\">{Escape(lines[0])}</text>");
            return;
        }

        svg.Append($"<text class=\"{cssClass}\" text-anchor=\"middle\">");
        for (var index = 0; index < lines.Count; index++)
            svg.Append($"<tspan x=\"{F(center.X)}\" y=\"{F(firstBaseline + index * TextMetrics.LineHeight)}\">{Escape(lines[index])}</tspan>");
        svg.Append("</text>");
    }

    private static string F(double value) =>
        Math.Abs(value) < 0.005 ? "0" : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private sealed record NodeBox(NodeShape Shape, IReadOnlyList<string> Lines, double Width, double Height, double CornerRadius);

    private sealed record LabelBox(IReadOnlyList<string> Lines, double Width, double Height);
}
