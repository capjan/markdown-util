using StaticCode.Diagrams.Flowchart;

namespace StaticCode.Diagrams.Context;

/// <summary>A system in the center, surrounded by the people and systems it is linked to.</summary>
public sealed class ContextDiagram
{
    public ContextDiagram(FlowchartNode center, IReadOnlyList<FlowchartNode> neighbors, IReadOnlyList<FlowchartEdge> edges, string? title)
    {
        Center = center;
        Neighbors = neighbors;
        Edges = edges;
        Title = title;
    }

    public FlowchartNode Center { get; }

    /// <summary>The surrounding people and systems in the order they first appear.</summary>
    public IReadOnlyList<FlowchartNode> Neighbors { get; }

    public IReadOnlyList<FlowchartEdge> Edges { get; }

    /// <summary>Accessible title from the <c>accTitle</c> statement.</summary>
    public string? Title { get; }
}

/// <summary>
/// Parses context diagrams. They use flowchart node and link syntax without a header, plus
/// <c>actor Id as Label</c> for people. The system in the center is the one that every link touches.
/// </summary>
public static class ContextParser
{
    public static ContextDiagram Parse(string source)
    {
        var graph = FlowchartParser.ParseContext(source);
        if (graph.Nodes.Count == 0) throw new DiagramSyntaxException("A context diagram needs at least one system.", 1);
        if (graph.Edges.Any(edge => edge.From == edge.To))
            throw new DiagramSyntaxException("A context diagram cannot link a system to itself.", 1);

        var center = graph.Nodes.FirstOrDefault(node => graph.Edges.All(edge => edge.From == node.Id || edge.To == node.Id))
                     ?? throw new DiagramSyntaxException("Every link must connect to the system in the center.", 1);
        return new ContextDiagram(center, graph.Nodes.Where(node => node != center).ToList(), graph.Edges, graph.Title);
    }
}
