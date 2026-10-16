using StaticCode.Diagrams.Flowchart;

namespace StaticCode.Diagrams.Test;

public class FlowchartParserTest
{
    [Theory]
    [InlineData("flowchart", FlowDirection.TopToBottom)]
    [InlineData("graph TD", FlowDirection.TopToBottom)]
    [InlineData("flowchart TB", FlowDirection.TopToBottom)]
    [InlineData("flowchart BT", FlowDirection.BottomToTop)]
    [InlineData("graph LR", FlowDirection.LeftToRight)]
    [InlineData("flowchart RL", FlowDirection.RightToLeft)]
    public void ParsesHeaderDirection(string header, FlowDirection direction)
    {
        var diagram = FlowchartParser.Parse($"{header}\n  A --> B");

        Assert.Equal(direction, diagram.Direction);
    }

    [Theory]
    [InlineData("A[Text]", NodeShape.Rectangle)]
    [InlineData("A(Text)", NodeShape.Rounded)]
    [InlineData("A([Text])", NodeShape.Stadium)]
    [InlineData("A((Text))", NodeShape.Circle)]
    [InlineData("A{Text}", NodeShape.Diamond)]
    public void ParsesNodeShapes(string node, NodeShape shape)
    {
        var diagram = FlowchartParser.Parse($"flowchart TD\n{node}");

        var parsed = Assert.Single(diagram.Nodes);
        Assert.Equal("A", parsed.Id);
        Assert.Equal("Text", parsed.Label);
        Assert.Equal(shape, parsed.Shape);
    }

    [Theory]
    [InlineData("A --> B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.Arrow, 1)]
    [InlineData("A --- B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.None, 1)]
    [InlineData("A ---> B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.Arrow, 2)]
    [InlineData("A -.-> B", LineStyle.Dotted, EdgeMarker.None, EdgeMarker.Arrow, 1)]
    [InlineData("A -..- B", LineStyle.Dotted, EdgeMarker.None, EdgeMarker.None, 2)]
    [InlineData("A ==> B", LineStyle.Thick, EdgeMarker.None, EdgeMarker.Arrow, 1)]
    [InlineData("A === B", LineStyle.Thick, EdgeMarker.None, EdgeMarker.None, 1)]
    [InlineData("A <--> B", LineStyle.Solid, EdgeMarker.Arrow, EdgeMarker.Arrow, 1)]
    [InlineData("A --o B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.Circle, 1)]
    [InlineData("A --x B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.Cross, 1)]
    [InlineData("A-->B", LineStyle.Solid, EdgeMarker.None, EdgeMarker.Arrow, 1)]
    public void ParsesLinks(string statement, LineStyle style, EdgeMarker start, EdgeMarker end, int length)
    {
        var diagram = FlowchartParser.Parse($"flowchart TD\n{statement}");

        Assert.Equal(new FlowchartEdge("A", "B", null, style, start, end, length), Assert.Single(diagram.Edges));
    }

    [Theory]
    [InlineData("A -->|Yes| B", LineStyle.Solid)]
    [InlineData("A --> |Yes| B", LineStyle.Solid)]
    [InlineData("A -- Yes --> B", LineStyle.Solid)]
    [InlineData("A -. Yes .-> B", LineStyle.Dotted)]
    [InlineData("A == Yes ==> B", LineStyle.Thick)]
    [InlineData("A -->|\"Yes\"| B", LineStyle.Solid)]
    public void ParsesLinkLabels(string statement, LineStyle style)
    {
        var edge = Assert.Single(FlowchartParser.Parse($"flowchart TD\n{statement}").Edges);

        Assert.Equal("Yes", edge.Label);
        Assert.Equal(style, edge.Style);
        Assert.Equal(EdgeMarker.Arrow, edge.EndMarker);
    }

    [Fact]
    public void ParsesChainsAndGroups()
    {
        var diagram = FlowchartParser.Parse("flowchart TD\nA & B --> C --> D & E");

        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, diagram.Nodes.Select(node => node.Id));
        Assert.Equal(
            new[] { "A>C", "B>C", "C>D", "C>E" },
            diagram.Edges.Select(edge => $"{edge.From}>{edge.To}"));
    }

    [Fact]
    public void KeepsTheLastShapeAndLabelOfANode()
    {
        var diagram = FlowchartParser.Parse("flowchart TD\nA --> B\nA{Decide} --> C");

        var node = diagram.Nodes[0];
        Assert.Equal("Decide", node.Label);
        Assert.Equal(NodeShape.Diamond, node.Shape);
    }

    [Fact]
    public void ParsesQuotedLabelsLineBreaksAndEntities()
    {
        var diagram = FlowchartParser.Parse("flowchart TD\nA[\"Run (once)\"] --> B[First<br>Second #quot;x#quot; #35;]");

        Assert.Equal("Run (once)", diagram.Nodes[0].Label);
        Assert.Equal("First\nSecond \"x\" #", diagram.Nodes[1].Label);
    }

    [Fact]
    public void SkipsCommentsStylingAndSemicolons()
    {
        var diagram = FlowchartParser.Parse("""
            %% A comment
            graph LR;
                A --> B;  B --> C
                classDef warm fill:#f96
                class A warm
                style B fill:#f9f
                linkStyle 0 stroke:#ff3
                click A href "https://example.com"
                C:::warm --> D
                accTitle: Build pipeline
            """);

        Assert.Equal(new[] { "A", "B", "C", "D" }, diagram.Nodes.Select(node => node.Id));
        Assert.Equal(3, diagram.Edges.Count);
        Assert.Equal("Build pipeline", diagram.Title);
    }

    [Theory]
    [InlineData("subgraph One\nA --> B\nend")]
    [InlineData("A[[Subroutine]]")]
    [InlineData("A[(Database)]")]
    [InlineData("A{{Hexagon}}")]
    [InlineData("A>Flag]")]
    [InlineData("A --> ")]
    [InlineData("A -- missing end B")]
    [InlineData("A[Unclosed")]
    public void RejectsUnsupportedSyntax(string statements)
    {
        var exception = Assert.Throws<DiagramSyntaxException>(() => FlowchartParser.Parse($"flowchart TD\n{statements}"));

        Assert.Equal(2, exception.Line);
    }

    [Theory]
    [InlineData("flowchart LR\nA --> B", true)]
    [InlineData("%% comment\n\ngraph\nA", true)]
    [InlineData("sequenceDiagram\nA->>B: Hi", false)]
    [InlineData("", false)]
    public void DetectsFlowcharts(string source, bool expected)
    {
        Assert.Equal(expected, FlowchartParser.CanParse(source));
    }
}
