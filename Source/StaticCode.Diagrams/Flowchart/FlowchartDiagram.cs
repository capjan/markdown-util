namespace StaticCode.Diagrams.Flowchart;

public enum FlowDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}

public enum NodeShape
{
    Rectangle,
    Rounded,
    Stadium,
    Circle,
    Diamond,

    /// <summary>A person, drawn as a stick figure; available in context diagrams.</summary>
    Actor
}

public enum LineStyle
{
    Solid,
    Dotted,
    Thick
}

public enum EdgeMarker
{
    None,
    Arrow,
    Circle,
    Cross
}

public sealed class FlowchartNode
{
    public FlowchartNode(string id)
    {
        Id = id;
        Label = id;
    }

    public string Id { get; }
    public string Label { get; set; }
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;
    public string? Href { get; set; }
}

public sealed class FlowchartGroup
{
    public FlowchartGroup(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }
    public string Label { get; }
    public List<string> NodeIds { get; } = new();
}

/// <param name="Length">Minimum number of ranks the edge spans; <c>---&gt;</c> is longer than <c>--&gt;</c>.</param>
public sealed record FlowchartEdge(
    string From,
    string To,
    string? Label,
    LineStyle Style,
    EdgeMarker StartMarker,
    EdgeMarker EndMarker,
    int Length);

public sealed class FlowchartDiagram
{
    public FlowchartDiagram(
        FlowDirection direction,
        IReadOnlyList<FlowchartNode> nodes,
        IReadOnlyList<FlowchartEdge> edges,
        string? title,
        IReadOnlyList<FlowchartGroup>? groups = null)
    {
        Direction = direction;
        Nodes = nodes;
        Edges = edges;
        Title = title;
        Groups = groups ?? Array.Empty<FlowchartGroup>();
    }

    public FlowDirection Direction { get; }
    public IReadOnlyList<FlowchartNode> Nodes { get; }
    public IReadOnlyList<FlowchartEdge> Edges { get; }

    /// <summary>Accessible title from Mermaid's <c>accTitle</c> statement.</summary>
    public string? Title { get; }
    public IReadOnlyList<FlowchartGroup> Groups { get; }
}
