using System.Text;
using StaticCode.Diagrams.Flowchart;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Svg;
using StaticCode.Diagrams.Text;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Context;

/// <summary>
/// Writes a context diagram as inline SVG with the flowchart node shapes and links. The neighbors sit at equal angles
/// on an ellipse around the center, clockwise from the top in the order they first appear. The ellipse is scaled to
/// the smallest size at which no shapes overlap and every link has room for its label.
/// </summary>
internal static class ContextSvgWriter
{
    private const double MinLinkLength = 64;
    private const double NeighborGap = 32;
    private const double LabelClearance = 14;
    private const double ParallelSpacing = 10;
    private const double CanvasPadding = 4;
    private const string CenterClass = "diagram-context-center";

    public static string Write(ContextDiagram diagram)
    {
        var centerBox = NodeShapes.CreateNode(diagram.Center);
        var boxes = diagram.Neighbors.Select(NodeShapes.CreateNode).ToList();
        var neighborIndex = diagram.Neighbors.Select((node, index) => (node.Id, index)).ToDictionary(item => item.Id, item => item.index);
        var labels = diagram.Edges.Select(edge => edge.Label == null ? null : NodeShapes.CreateLabel(edge.Label)).ToList();
        int NeighborOf(FlowchartEdge edge) => neighborIndex[edge.From == diagram.Center.Id ? edge.To : edge.From];

        var linkLabels = boxes.Select(_ => new List<LabelBox>()).ToArray();
        for (var index = 0; index < diagram.Edges.Count; index++)
            if (labels[index] is { } label)
                linkLabels[NeighborOf(diagram.Edges[index])].Add(label);
        var positions = PlaceNeighbors(centerBox, boxes, linkLabels);

        Point PositionOf(string id) => id == diagram.Center.Id ? new Point(0, 0) : positions[neighborIndex[id]];
        NodeBox BoxOf(string id) => id == diagram.Center.Id ? centerBox : boxes[neighborIndex[id]];

        var edges = new StringBuilder();
        var labelLayer = new StringBuilder();
        var bounds = new Bounds();
        bounds.Include(new Point(0, 0), centerBox.Width, centerBox.Height);
        for (var index = 0; index < boxes.Count; index++) bounds.Include(positions[index], boxes[index].Width, boxes[index].Height);

        // Several links between the same two shapes run side by side.
        var linkCounts = new int[boxes.Count];
        var linkTotals = diagram.Edges.GroupBy(NeighborOf).ToDictionary(group => group.Key, group => group.Count());
        for (var index = 0; index < diagram.Edges.Count; index++)
        {
            var edge = diagram.Edges[index];
            var neighbor = NeighborOf(edge);
            var offset = (linkCounts[neighbor]++ - (linkTotals[neighbor] - 1) / 2.0) * ParallelSpacing;

            var from = PositionOf(edge.From);
            var to = PositionOf(edge.To);
            var direction = (to - from).Normalized;
            var shift = direction.Perpendicular * offset;
            var start = from + direction * NodeShapes.Boundary(BoxOf(edge.From), direction) + shift;
            var end = to - direction * NodeShapes.Boundary(BoxOf(edge.To), direction * -1) + shift;
            var startMarker = NodeShapes.Marker(edge.StartMarker);
            var endMarker = NodeShapes.Marker(edge.EndMarker);
            var scale = NodeShapes.MarkerScale(edge.Style);
            var lineStart = start + direction * SvgMarkers.Inset(startMarker, scale);
            var lineEnd = end - direction * SvgMarkers.Inset(endMarker, scale);

            edges.Append($"<path class=\"{NodeShapes.EdgeClass(edge.Style)}\" d=\"M{F(lineStart.X)} {F(lineStart.Y)} L{F(lineEnd.X)} {F(lineEnd.Y)}\"/>");
            SvgMarkers.Write(edges, endMarker, end, direction, scale);
            SvgMarkers.Write(edges, startMarker, start, direction * -1, scale);
            edges.Append('\n');

            if (labels[index] is not { } label) continue;
            var center = (start + end) * 0.5;
            NodeShapes.WriteLabel(labelLayer, label, center);
            bounds.Include(center, label.Width, label.Height);
        }

        var width = bounds.MaxX - bounds.MinX + 2 * CanvasPadding;
        var height = bounds.MaxY - bounds.MinY + 2 * CanvasPadding;
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-context\"")
            .Append($" viewBox=\"{F(bounds.MinX - CanvasPadding)} {F(bounds.MinY - CanvasPadding)} {F(width)} {F(height)}\"")
            .Append($" width=\"{F(width)}\" height=\"{F(height)}\"")
            .Append($" font-size=\"{F(TextMetrics.FontSize)}\" role=\"img\" aria-label=\"{Escape(diagram.Title ?? "Context diagram")}\">\n")
            .Append("<g class=\"diagram-edges\">\n").Append(edges)
            .Append("</g>\n<g class=\"diagram-edge-labels\">\n").Append(labelLayer)
            .Append("</g>\n<g class=\"diagram-nodes\">\n");
        for (var index = 0; index < boxes.Count; index++) NodeShapes.WriteNode(svg, boxes[index], positions[index]);
        NodeShapes.WriteNode(svg, centerBox, new Point(0, 0), CenterClass);
        svg.Append("</g>\n</svg>");
        return svg.ToString();
    }

    /// <summary>
    /// Places the neighbors at <c>s · (stretch · cos θ, sin θ)</c> for evenly spaced angles θ. Both constraints are
    /// linear in the scale s: every neighbor must keep a link-sized gap to the center, and adjacent neighbors must not
    /// touch. The smallest s that satisfies all of them is therefore just their maximum.
    /// </summary>
    private static Point[] PlaceNeighbors(NodeBox center, List<NodeBox> boxes, List<LabelBox>[] linkLabels)
    {
        var count = boxes.Count;
        if (count == 0) return Array.Empty<Point>();

        // Wide boxes waste less space on an ellipse that is stretched horizontally.
        var all = boxes.Append(center).ToList();
        var stretch = Math.Clamp((all.Average(box => box.Width) + 60) / (all.Average(box => box.Height) + 60), 1, 1.6);
        var units = Enumerable.Range(0, count)
            .Select(index => -Math.PI / 2 + 2 * Math.PI * index / count)
            .Select(angle => new Point(stretch * Math.Cos(angle), Math.Sin(angle)))
            .ToArray();

        var scale = 0.0;
        for (var index = 0; index < count; index++)
        {
            var direction = units[index].Normalized;
            var labelLength = linkLabels[index]
                .Select(label => Math.Abs(direction.X) * label.Width + Math.Abs(direction.Y) * label.Height)
                .DefaultIfEmpty(0)
                .Max();
            var gap = Math.Max(MinLinkLength, labelLength + 2 * LabelClearance);
            var required = NodeShapes.Reach(center, direction) + NodeShapes.Reach(boxes[index], direction) + gap;
            scale = Math.Max(scale, required / units[index].Length);
        }

        for (var index = 0; index < count && count > 1; index++)
        {
            var next = (index + 1) % count;
            if (count == 2 && next == 0) break;
            var difference = units[next] - units[index];
            var required = NodeShapes.Reach(boxes[index], difference) + NodeShapes.Reach(boxes[next], difference) + NeighborGap;
            scale = Math.Max(scale, required / difference.Length);
        }

        return units.Select(unit => unit * scale).ToArray();
    }

    private sealed class Bounds
    {
        public double MinX { get; private set; } = double.PositiveInfinity;
        public double MinY { get; private set; } = double.PositiveInfinity;
        public double MaxX { get; private set; } = double.NegativeInfinity;
        public double MaxY { get; private set; } = double.NegativeInfinity;

        public void Include(Point center, double width, double height)
        {
            MinX = Math.Min(MinX, center.X - width / 2);
            MinY = Math.Min(MinY, center.Y - height / 2);
            MaxX = Math.Max(MaxX, center.X + width / 2);
            MaxY = Math.Max(MaxY, center.Y + height / 2);
        }
    }
}
