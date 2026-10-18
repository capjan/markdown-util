using System.Text;
using StaticCode.Diagrams.Flowchart;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Svg;
using StaticCode.Diagrams.Text;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Component;

/// <summary>
/// Lays out and writes a component diagram. Containers are laid out from the inside out: each container's children
/// get their own layered layout, and the container then takes part in its parent's layout as one box. A link that
/// crosses a container's border enters or leaves through a port node pinned to the first or last rank inside, and the
/// parent routes the link to that port's position on the border, so the pieces join into one route.
/// </summary>
internal sealed class ComponentSvgWriter
{
    private const double CanvasPadding = 4;
    private const double TitleHeight = TextMetrics.LineHeight + 14;

    private readonly ComponentDiagram _diagram;
    private readonly Dictionary<string, ComponentElement> _elements;
    private readonly Dictionary<string, ElementBox> _boxes;
    private readonly List<RoutedLink> _links;
    private readonly Dictionary<string, ContainerLayout> _containers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Point> _centers = new(StringComparer.Ordinal);
    private readonly bool _horizontal;
    private readonly Point _axis;
    private readonly Point _cross;
    private ContainerLayout? _root;

    private ComponentSvgWriter(ComponentDiagram diagram)
    {
        _diagram = diagram;
        _elements = diagram.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
        _boxes = diagram.Elements.Where(element => !element.IsContainer).ToDictionary(element => element.Id, ComponentShapes.Create, StringComparer.Ordinal);
        _horizontal = diagram.LeftToRight;
        _axis = _horizontal ? new Point(1, 0) : new Point(0, 1);
        _cross = _horizontal ? new Point(0, 1) : new Point(1, 0);

        // Up and left links are laid out as down and right links in the other direction.
        _links = diagram.Links.Select((link, index) =>
        {
            var reversed = link.Direction is LinkDirection.Up or LinkDirection.Left;
            return new RoutedLink(
                index,
                link,
                reversed ? link.To : link.From,
                reversed ? link.From : link.To,
                reversed,
                link.Direction is LinkDirection.Left or LinkDirection.Right);
        }).ToList();

        foreach (var link in _links)
            if (Containers(link.From).Contains(link.To) || Containers(link.To).Contains(link.From))
                throw new DiagramSyntaxException("A link cannot connect a container with its own content.", 1);
    }

    public static string Write(ComponentDiagram diagram) => new ComponentSvgWriter(diagram).Write();

    private string Write()
    {
        var root = _root = LayoutContainer(null);
        var top = _diagram.Title == null ? 0 : TitleHeight;
        Place(root, new Point(0, top));

        var containers = new StringBuilder();
        foreach (var element in _diagram.Elements.Where(element => element.IsContainer).OrderBy(element => Containers(element.Id).Count))
        {
            var layout = _containers[element.Id];
            var center = _centers[element.Id];
            ComponentShapes.WriteContainer(containers, element, center.X - layout.Width / 2, center.Y - layout.Height / 2, layout.Width, layout.Height);
        }

        var edges = new StringBuilder();
        var labels = new StringBuilder();
        var minX = 0.0;
        var maxX = root.ContentWidth;
        foreach (var link in _links)
        {
            if (link.Link.Hidden) continue;
            var (points, labelCenter) = Route(link);
            if (points.Count < 2) continue;
            WriteLink(edges, link, points);
            if (link.Link.Label == null || labelCenter is not { } center) continue;
            var label = NodeShapes.CreateLabel(link.Link.Label);
            NodeShapes.WriteLabel(labels, label, center);
            minX = Math.Min(minX, center.X - label.Width / 2);
            maxX = Math.Max(maxX, center.X + label.Width / 2);
        }

        var nodes = new StringBuilder();
        foreach (var element in _diagram.Elements.Where(element => !element.IsContainer))
            ComponentShapes.WriteElement(nodes, _boxes[element.Id], _centers[element.Id], element.Href);

        var title = new StringBuilder();
        if (_diagram.Title != null)
        {
            var lines = TextMetrics.Wrap(_diagram.Title, double.MaxValue);
            var width = TextMetrics.Measure(lines).Width;
            var center = root.ContentWidth / 2;
            minX = Math.Min(minX, center - width / 2);
            maxX = Math.Max(maxX, center + width / 2);
            WriteText(title, lines, new Point(center, TitleHeight / 2 - 2), "diagram-component-title");
        }

        var svgWidth = maxX - minX + 2 * CanvasPadding;
        var svgHeight = top + root.ContentHeight + 2 * CanvasPadding;
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-component\"")
            .Append($" viewBox=\"{F(minX - CanvasPadding)} {F(-CanvasPadding)} {F(svgWidth)} {F(svgHeight)}\"")
            .Append($" width=\"{F(svgWidth)}\" height=\"{F(svgHeight)}\"")
            .Append($" font-size=\"{F(TextMetrics.FontSize)}\" role=\"img\" aria-label=\"{Escape(_diagram.Title ?? "Component diagram")}\">\n")
            .Append("<g class=\"diagram-containers\">\n").Append(containers)
            .Append("</g>\n<g class=\"diagram-edges\">\n").Append(edges)
            .Append("</g>\n<g class=\"diagram-edge-labels\">\n").Append(labels)
            .Append("</g>\n<g class=\"diagram-nodes\">\n").Append(nodes)
            .Append("</g>\n").Append(title)
            .Append("</svg>");
        return svg.ToString();
    }

    /// <summary>Lays out the children of a container (or of the diagram for <c>null</c>), after their own content.</summary>
    private ContainerLayout LayoutContainer(string? containerId)
    {
        var children = _diagram.Elements.Where(element => element.Parent == containerId).ToList();
        foreach (var child in children.Where(child => child.IsContainer)) _containers[child.Id] = LayoutContainer(child.Id);

        var nodes = children.Select(child => child.IsContainer
            ? new LayoutNode(_containers[child.Id].Width, _containers[child.Id].Height)
            : new LayoutNode(_boxes[child.Id].Width, _boxes[child.Id].Height)).ToList();
        var index = children.Select((child, position) => (child.Id, position)).ToDictionary(item => item.Id, item => item.position, StringComparer.Ordinal);
        var edges = new List<LayoutEdge>();
        var roles = new List<(int Link, Role Role)>();

        foreach (var link in _links)
        {
            var from = ChildOf(containerId, link.From);
            var to = ChildOf(containerId, link.To);
            if (from == null && to == null) continue;
            if (from != null && to != null && from == to) continue;

            var sourcePort = from != null && from != link.From ? _containers[from].Ports[(link.Index, Role.Exit)] : (double?)null;
            var targetPort = to != null && to != link.To ? _containers[to].Ports[(link.Index, Role.Enter)] : (double?)null;
            if (from != null && to != null)
            {
                var flat = link.Flat && from == link.From && to == link.To;
                var label = link.Link.Label == null || link.Link.Hidden ? null : NodeShapes.CreateLabel(link.Link.Label);
                edges.Add(new LayoutEdge(index[from], index[to], flat ? 0 : link.Link.Length, label?.Width ?? 0, label?.Height ?? 0, sourcePort, targetPort));
                roles.Add((link.Index, Role.Through));
            }
            else if (from != null)
            {
                nodes.Add(new LayoutNode(0, 0, Pin: LayoutPin.Last));
                edges.Add(new LayoutEdge(index[from], nodes.Count - 1, SourcePort: sourcePort));
                roles.Add((link.Index, Role.Exit));
            }
            else
            {
                nodes.Add(new LayoutNode(0, 0, Pin: LayoutPin.First));
                edges.Add(new LayoutEdge(nodes.Count - 1, index[to!], TargetPort: targetPort));
                roles.Add((link.Index, Role.Enter));
            }
        }

        var result = new LayeredLayout().Compute(nodes, edges, _horizontal ? LayoutDirection.LeftToRight : LayoutDirection.TopToBottom);
        var layout = new ContainerLayout(result.Width, result.Height);
        for (var position = 0; position < children.Count; position++) layout.Centers[children[position].Id] = result.NodeCenters[position];
        for (var edge = 0; edge < edges.Count; edge++) layout.Routes[roles[edge]] = result.Edges[edge];

        if (containerId != null) FitContainer(_elements[containerId], layout, result, children, edges, roles);
        return layout;
    }

    /// <summary>
    /// Sizes a container box around its children and the label. The port ranks above and below the children are cut
    /// off: routes to a port end at the box border instead, where the parent's route continues.
    /// </summary>
    private void FitContainer(
        ComponentElement element, ContainerLayout layout, LayoutResult result, List<ComponentElement> children,
        List<LayoutEdge> edges, List<(int Link, Role Role)> roles)
    {
        double Along(Point point) => point.Dot(_axis);
        double Across(Point point) => point.Dot(_cross);

        var alongMin = 0.0;
        var alongMax = 0.0;
        if (children.Count > 0)
        {
            alongMin = double.PositiveInfinity;
            alongMax = double.NegativeInfinity;
            for (var position = 0; position < children.Count; position++)
            {
                var size = SizeOf(children[position]);
                var half = (_horizontal ? size.Width : size.Height) / 2;
                alongMin = Math.Min(alongMin, Along(result.NodeCenters[position]) - half);
                alongMax = Math.Max(alongMax, Along(result.NodeCenters[position]) + half);
            }
        }

        // The label sits above the content, which is before the content in top-to-bottom layouts and beside it otherwise.
        var header = ComponentShapes.HeaderHeight(element);
        var padding = ComponentShapes.ContainerPadding;
        var before = _horizontal ? padding : header;
        var crossBefore = _horizontal ? header : padding;
        var crossExtent = _horizontal ? result.Height : result.Width;
        var alongLength = alongMax - alongMin + before + padding;
        var crossLength = crossExtent + crossBefore + padding;
        layout.Width = Math.Max(_horizontal ? alongLength : crossLength, ComponentShapes.MinContainerWidth(element));
        layout.Height = _horizontal ? crossLength : alongLength;
        layout.Origin = _horizontal
            ? new Point(before - alongMin, crossBefore)
            : new Point((layout.Width - crossExtent) / 2, before - alongMin);

        var borderBefore = alongMin - before;
        var borderAfter = alongMax + padding;
        for (var edge = 0; edge < edges.Count; edge++)
        {
            var role = roles[edge].Role;
            if (role == Role.Through) continue;

            var route = layout.Routes[roles[edge]];
            var port = result.NodeCenters[role == Role.Exit ? edges[edge].Target : edges[edge].Source];
            var border = _cross * Across(port) + _axis * (role == Role.Exit ? borderAfter : borderBefore);
            var points = role == Role.Exit
                ? route.Points.TakeWhile(point => Along(point) < borderAfter).Append(border)
                : route.Points.SkipWhile(point => Along(point) <= borderBefore).Prepend(border);
            layout.Routes[roles[edge]] = route with { Points = points.ToList() };

            // Port positions along the border, relative to the container's center, for the parent's layout.
            layout.Ports[roles[edge]] = _horizontal
                ? layout.Origin.Y + port.Y - layout.Height / 2
                : layout.Origin.X + port.X - layout.Width / 2;
        }
    }

    private (double Width, double Height) SizeOf(ComponentElement element) =>
        element.IsContainer ? (_containers[element.Id].Width, _containers[element.Id].Height) : (_boxes[element.Id].Width, _boxes[element.Id].Height);

    private void Place(ContainerLayout layout, Point origin)
    {
        layout.AbsoluteOrigin = origin;
        foreach (var (id, center) in layout.Centers)
        {
            var absolute = origin + center;
            _centers[id] = absolute;
            if (!_containers.TryGetValue(id, out var child)) continue;
            Place(child, new Point(absolute.X - child.Width / 2, absolute.Y - child.Height / 2) + child.Origin);
        }
    }

    /// <summary>Joins the pieces of a link's route: out of its source's containers, across, and into its target's.</summary>
    private (List<Point> Points, Point? Label) Route(RoutedLink link)
    {
        var fromContainers = Containers(link.From);
        var toContainers = Containers(link.To);
        var common = fromContainers.FirstOrDefault(toContainers.Contains);

        var points = new List<Point>();
        Point? label = null;
        void Append(string? containerId, Role role)
        {
            var layout = containerId == null ? _root! : _containers[containerId];
            if (!layout.Routes.TryGetValue((link.Index, role), out var route)) return;
            foreach (var point in route.Points)
            {
                var absolute = layout.AbsoluteOrigin + point;
                if (points.Count == 0 || (points[^1] - absolute).Length > 0.5) points.Add(absolute);
            }

            if (role == Role.Through && route.LabelCenter is { } center) label = layout.AbsoluteOrigin + center;
        }

        foreach (var container in fromContainers.TakeWhile(container => container != common)) Append(container, Role.Exit);
        Append(common, Role.Through);
        foreach (var container in toContainers.TakeWhile(container => container != common).Reverse()) Append(container, Role.Enter);

        if (points.Count >= 2)
        {
            points[0] = Snap(link.From, points[0]);
            points[^1] = Snap(link.To, points[^1]);
        }

        if (link.Reversed) points.Reverse();
        return (points, label);
    }

    /// <summary>Links attach to an interface's circle rather than to its label.</summary>
    private Point Snap(string id, Point point)
    {
        if (!_boxes.TryGetValue(id, out var box) || box.Kind != ElementKind.Interface) return point;
        var circle = ComponentShapes.InterfaceCenter(box, _centers[id]);
        var offset = point - circle;
        if (!_horizontal && offset.Y > ComponentShapes.InterfaceRadiusValue + 1) return point;
        return circle + offset.Normalized * ComponentShapes.InterfaceRadiusValue;
    }

    private void WriteLink(StringBuilder svg, RoutedLink link, List<Point> points)
    {
        var fromHead = Marker(link.Link.FromHead);
        var toHead = Marker(link.Link.ToHead);
        var startDirection = EndDirection(points[0], points[1]);
        var endDirection = EndDirection(points[^2], points[^1]);
        var start = points[0];
        var end = points[^1];
        points[0] = start + startDirection * SvgMarkers.Inset(fromHead);
        points[^1] = end - endDirection * SvgMarkers.Inset(toHead);

        var path = new StringBuilder($"M{F(points[0].X)} {F(points[0].Y)}");
        if (points.Count == 2)
        {
            path.Append($" L{F(points[1].X)} {F(points[1].Y)}");
        }
        else
        {
            // A smooth spline through the route; the ends leave and enter elements along the rank direction.
            var tangents = new Point[points.Count];
            tangents[0] = startDirection * (Math.Abs((points[1] - points[0]).Dot(startDirection)) * 1.5);
            tangents[^1] = endDirection * (Math.Abs((points[^1] - points[^2]).Dot(endDirection)) * 1.5);
            for (var index = 1; index < points.Count - 1; index++) tangents[index] = (points[index + 1] - points[index - 1]) * 0.5;
            for (var index = 0; index < points.Count - 1; index++)
            {
                var c1 = points[index] + tangents[index] * (1.0 / 3);
                var c2 = points[index + 1] - tangents[index + 1] * (1.0 / 3);
                path.Append($" C{F(c1.X)} {F(c1.Y)} {F(c2.X)} {F(c2.Y)} {F(points[index + 1].X)} {F(points[index + 1].Y)}");
            }
        }

        var lineClass = link.Link.Line switch
        {
            LinkLine.Dotted => "diagram-edge diagram-edge-dotted",
            LinkLine.Bold => "diagram-edge diagram-edge-thick",
            _ => "diagram-edge"
        };
        svg.Append($"<path class=\"{lineClass}\" d=\"{path}\"/>");
        SvgMarkers.Write(svg, toHead, end, endDirection);
        SvgMarkers.Write(svg, fromHead, start, startDirection * -1);
        svg.Append('\n');
    }

    /// <summary>Routes leave and enter along the rank axis; straight side-to-side routes keep their own direction.</summary>
    private Point EndDirection(Point from, Point to)
    {
        var difference = to - from;
        var along = difference.Dot(_axis);
        return Math.Abs(along) >= Math.Abs(difference.Dot(_cross)) * 0.25 && Math.Abs(along) > 0.01
            ? _axis * Math.Sign(along)
            : difference.Normalized;
    }

    private static MarkerKind Marker(LinkHead head) => head switch
    {
        LinkHead.Arrow => MarkerKind.Arrow,
        LinkHead.Triangle => MarkerKind.Triangle,
        _ => MarkerKind.None
    };

    /// <summary>The containers around an element, innermost first.</summary>
    private List<string> Containers(string id)
    {
        var result = new List<string>();
        for (var parent = _elements[id].Parent; parent != null; parent = _elements[parent].Parent) result.Add(parent);
        return result;
    }

    /// <summary>The direct child of a container (or of the diagram) that holds an element, or <c>null</c> if it is outside.</summary>
    private string? ChildOf(string? containerId, string id)
    {
        for (var current = id; current != null; current = _elements[current].Parent)
            if (_elements[current].Parent == containerId)
                return current;
        return null;
    }

    private enum Role
    {
        Through,
        Exit,
        Enter
    }

    private sealed record RoutedLink(int Index, ComponentLink Link, string From, string To, bool Reversed, bool Flat);

    private sealed class ContainerLayout
    {
        public ContainerLayout(double contentWidth, double contentHeight)
        {
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
            Width = contentWidth;
            Height = contentHeight;
        }

        public double ContentWidth { get; }
        public double ContentHeight { get; }

        /// <summary>The size of the container box; equal to the content for the diagram itself.</summary>
        public double Width { get; set; }

        public double Height { get; set; }

        /// <summary>Where the content starts inside the box.</summary>
        public Point Origin { get; set; }

        public Point AbsoluteOrigin { get; set; }
        public Dictionary<string, Point> Centers { get; } = new(StringComparer.Ordinal);
        public Dictionary<(int Link, Role Role), EdgeRoute> Routes { get; } = new();
        public Dictionary<(int Link, Role Role), double> Ports { get; } = new();
    }
}
