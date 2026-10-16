using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StaticCode.Diagrams.Context;
using StaticCode.Diagrams.Flowchart;

namespace StaticCode.Diagrams.Test;

public class ContextDiagramTest
{
    private const string Sample = """
        accTitle: Mobile Capture context
        MC[Mobile Capture]
        MC <--> IdP[Zuvex<br>IdP]
        MC <--> DRiVe[DRiVe Verfahren]
        MC --> DB[Datenbank]
        MC --> Mail[Dataport<br>E-Mail Server]
        MC --> AD[Active Directory]
        actor LK as Lehrkräfte
        actor SL as Schulleitung
        LK -. Verwendet .-> MC
        SL -.-> MC
        """;

    [Fact]
    public void FindsTheSystemInTheCenter()
    {
        var diagram = ContextParser.Parse(Sample);

        Assert.Equal("MC", diagram.Center.Id);
        Assert.Equal(new[] { "IdP", "DRiVe", "DB", "Mail", "AD", "LK", "SL" }, diagram.Neighbors.Select(node => node.Id));
        Assert.Equal("Mobile Capture context", diagram.Title);
    }

    [Fact]
    public void ParsesActors()
    {
        var diagram = ContextParser.Parse(Sample);

        var teachers = diagram.Neighbors.Single(node => node.Id == "LK");
        Assert.Equal(NodeShape.Actor, teachers.Shape);
        Assert.Equal("Lehrkräfte", teachers.Label);
        Assert.Equal("Verwendet", diagram.Edges.Single(edge => edge.From == "LK").Label);
    }

    [Theory]
    [InlineData("A --> B", "A")]
    [InlineData("A --> B\nC --> B", "B")]
    [InlineData("actor U\nU --> S[System]", "U")]
    [InlineData("S[System]", "S")]
    public void PrefersTheFirstCandidateForTheCenter(string source, string center)
    {
        Assert.Equal(center, ContextParser.Parse(source).Center.Id);
    }

    [Theory]
    [InlineData("A --> B\nC --> D")]
    [InlineData("A --> A")]
    [InlineData("%% only a comment")]
    [InlineData("actor")]
    [InlineData("subgraph One\nA --> B\nend")]
    public void RejectsInvalidContextDiagrams(string source)
    {
        Assert.Throws<DiagramSyntaxException>(() => ContextParser.Parse(source));
    }

    [Fact]
    public void KeepsActorsOutOfFlowcharts()
    {
        Assert.Throws<DiagramSyntaxException>(() => FlowchartParser.Parse("flowchart TD\nactor A as Person"));
    }

    [Fact]
    public void RendersWellFormedThemeableSvg()
    {
        var svg = DiagramRenderer.RenderContextSvg(Sample);

        var root = XElement.Parse(svg);
        Assert.Equal("diagram-context", root.Attribute("class")?.Value);
        Assert.Equal("Mobile Capture context", root.Attribute("aria-label")?.Value);
        Assert.DoesNotMatch(new Regex(@"\b(fill|stroke|style)="), svg);
        Assert.DoesNotContain("#", svg);
        Assert.Contains("diagram-context-center", svg);
        Assert.Equal(2, Regex.Matches(svg, "class=\"diagram-actor-head\"").Count);
        Assert.Contains("diagram-edge-dotted", svg);
        Assert.Contains("diagram-edge-label-text", svg);
    }

    [Fact]
    public void PlacesNeighborsClockwiseFromTheTop()
    {
        var boxes = NodeRectangles(DiagramRenderer.RenderContextSvg("C[Center]\nC --> N[North]\nC --> E[East]\nC --> S[South]\nC --> W[West]"));

        var center = boxes["Center"];
        Assert.Equal(center.X, boxes["North"].X, 1);
        Assert.True(boxes["North"].Y < center.Y);
        Assert.True(boxes["East"].X > center.X);
        Assert.Equal(center.Y, boxes["East"].Y, 1);
        Assert.True(boxes["South"].Y > center.Y);
        Assert.True(boxes["West"].X < center.X);
    }

    [Fact]
    public void KeepsAllShapesApart()
    {
        var source = "C[A rather wide system in the center]\n" +
                     string.Join('\n', Enumerable.Range(1, 12).Select(index => $"C --> N{index}[Neighbor number {index}]"));

        var boxes = NodeRectangles(DiagramRenderer.RenderContextSvg(source)).Values.ToList();

        Assert.Equal(13, boxes.Count);
        for (var i = 0; i < boxes.Count; i++)
        for (var j = i + 1; j < boxes.Count; j++)
            Assert.False(Overlap(boxes[i], boxes[j]), $"Boxes {i} and {j} overlap.");
    }

    [Fact]
    public void RoutesFencedBlocksByLanguage()
    {
        Assert.True(DiagramRenderer.TryRenderSvg("context", "A --> B", out var context));
        Assert.Contains("diagram-context", context);
        Assert.True(DiagramRenderer.TryRenderSvg("Mermaid", "flowchart LR\nA --> B", out var mermaid));
        Assert.Contains("diagram-flowchart", mermaid);
        Assert.False(DiagramRenderer.TryRenderSvg("context", "A --> B\nC --> D", out var invalid));
        Assert.Null(invalid);
        Assert.False(DiagramRenderer.TryRenderSvg("csharp", "var a = 1;", out _));
    }

    private static Dictionary<string, Box> NodeRectangles(string svg)
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        return XElement.Parse(svg).Descendants(ns + "g")
            .Where(group => group.Attribute("class")?.Value.Contains("diagram-node-rectangle") == true)
            .ToDictionary(
                group => group.Element(ns + "text")!.Value,
                group =>
                {
                    var rect = group.Element(ns + "rect")!;
                    double Value(string name) => double.Parse(rect.Attribute(name)!.Value, CultureInfo.InvariantCulture);
                    return new Box(Value("x") + Value("width") / 2, Value("y") + Value("height") / 2, Value("width"), Value("height"));
                });
    }

    private static bool Overlap(Box a, Box b) =>
        Math.Abs(a.X - b.X) < (a.Width + b.Width) / 2 && Math.Abs(a.Y - b.Y) < (a.Height + b.Height) / 2;

    private sealed record Box(double X, double Y, double Width, double Height);
}
