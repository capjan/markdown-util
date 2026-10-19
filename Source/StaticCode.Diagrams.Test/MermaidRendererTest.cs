using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StaticCode.Diagrams.Test;

public class MermaidRendererTest
{
    private const string Sample = """
        flowchart TD
            Start([Start]) --> Check{Valid?}
            Check -->|Yes| Save[Save]
            Check -. No .-> Fix(Fix input)
            Fix --> Check
            Save ==> Done((Done))
            Done --o Log
            Done --x Drop
            Log <--> Log
        """;

    [Fact]
    public void RendersWellFormedSvg()
    {
        var svg = MermaidRenderer.RenderSvg(Sample);

        var root = XElement.Parse(svg);
        Assert.Equal("svg", root.Name.LocalName);
        Assert.NotNull(root.Attribute("viewBox"));
        Assert.Equal("img", root.Attribute("role")?.Value);
    }

    [Fact]
    public void LeavesAllColorsToTheStylesheet()
    {
        var svg = MermaidRenderer.RenderSvg(Sample);

        Assert.DoesNotMatch(new Regex(@"\b(fill|stroke|style)="), svg);
        Assert.DoesNotContain("#", svg);
    }

    [Fact]
    public void WritesThemeClassesForEveryElement()
    {
        var svg = MermaidRenderer.RenderSvg(Sample);

        foreach (var cssClass in new[]
                 {
                     "diagram-node-stadium", "diagram-node-diamond", "diagram-node-rectangle", "diagram-node-rounded",
                     "diagram-node-circle", "diagram-node-shape", "diagram-node-text", "diagram-edge-dotted",
                     "diagram-edge-thick", "diagram-marker", "diagram-marker-cross", "diagram-edge-label-background",
                     "diagram-edge-label-text"
                 })
            Assert.Contains(cssClass, svg);
    }

    [Fact]
    public void EscapesLabels()
    {
        var svg = MermaidRenderer.RenderSvg("flowchart LR\nA[\"a < b & c\"] -->|\"x > y\"| B");

        Assert.Contains("a &lt; b &amp; c", svg);
        Assert.Contains("x &gt; y", svg);
    }

    [Fact]
    public void WrapsLongLabelsIntoLines()
    {
        var svg = MermaidRenderer.RenderSvg("flowchart TD\nA[This label is long enough to be wrapped onto more than one line]");

        Assert.True(Regex.Matches(svg, "<tspan").Count >= 2);
    }

    [Fact]
    public void UsesTheAccessibleTitle()
    {
        var svg = MermaidRenderer.RenderSvg("flowchart TD\naccTitle: Order flow\nA --> B");

        Assert.Contains("aria-label=\"Order flow\"", svg);
    }

    [Fact]
    public void RendersClassDiagramRelationships()
    {
        var svg = MermaidRenderer.RenderSvg("classDiagram\nAnimal <|-- Duck\nCar *-- Engine\nPerson ..> Address");

        Assert.Contains("Animal", svg);
        Assert.Contains("Duck", svg);
        Assert.Contains("diagram-edge-dotted", svg);
    }

    [Theory]
    [InlineData("Animal <|-- Duck")]
    [InlineData("Duck --|> Animal")]
    [InlineData("Animal <|.. Duck")]
    [InlineData("Duck ..|> Animal")]
    public void PlacesTheSuperclassAboveTheSubclassWithATriangleAtTheSuperclass(string relationship)
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg($"classDiagram\n{relationship}"));
        var animal = NodeShape(root, "Animal");
        var duck = NodeShape(root, "Duck");
        var animalTop = double.Parse(animal.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var animalBottom = animalTop + double.Parse(animal.Attribute("height")!.Value, CultureInfo.InvariantCulture);
        var duckTop = double.Parse(duck.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var triangle = Assert.Single(root.Descendants().Where(element => element.Attribute("class")?.Value == "diagram-marker-hollow"));
        var tip = Regex.Match(triangle.Attribute("d")!.Value, @"^M(?<x>-?\d+(?:\.\d+)?) (?<y>-?\d+(?:\.\d+)?)");

        Assert.True(animalTop < duckTop);
        Assert.True(tip.Success);
        var tipY = double.Parse(tip.Groups["y"].Value, CultureInfo.InvariantCulture);
        Assert.Equal(animalBottom, tipY, 2);
    }

    private static XElement NodeShape(XElement root, string label) => root.Descendants()
        .Single(element => element.Attribute("class")?.Value.StartsWith("diagram-node ", StringComparison.Ordinal) == true
                           && element.Descendants().Any(text => text.Attribute("class")?.Value == "diagram-node-text" && text.Value == label))
        .Elements().Single(element => element.Attribute("class")?.Value == "diagram-node-shape");

    [Fact]
    public void RendersClassDiagramSyntaxUsedByTheWiki()
    {
        const string source = """
            classDiagram
            classA <|-- classB
            classC *-- classD
            classE o-- classF
            classG <-- classH
            classI -- classJ
            classK <.. classL
            classM <|.. classN
            classO .. classP
            """;

        var svg = MermaidRenderer.RenderSvg(source);

        Assert.Contains("classP", svg);
        Assert.Contains("diagram-edge-dotted", svg);
    }

    [Fact]
    public void RendersConfiguredSubgraphsAndNamespacedNodes()
    {
        var source = """
            ---
            config:
              layout: elk
            ---
            flowchart TB
            subgraph environment["environment"]
              target:App("App")
              product:App[["App package"]]
            end
            product:App --> target:App
            """;

        var svg = MermaidRenderer.RenderSvg(source);

        Assert.Contains("diagram-subgraph", svg);
        Assert.Contains("environment", svg);
        Assert.Contains("App package", svg);

        var root = XElement.Parse(svg);
        var viewBox = root.Attribute("viewBox")!.Value.Split(' ')
            .Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        var group = root.Descendants().Single(element => element.Attribute("class")?.Value == "diagram-subgraph");
        var left = double.Parse(group.Attribute("x")!.Value, CultureInfo.InvariantCulture);
        var top = double.Parse(group.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var right = left + double.Parse(group.Attribute("width")!.Value, CultureInfo.InvariantCulture);
        var bottom = top + double.Parse(group.Attribute("height")!.Value, CultureInfo.InvariantCulture);
        Assert.True(left >= viewBox[0]);
        Assert.True(top >= viewBox[1]);
        Assert.True(right <= viewBox[0] + viewBox[2]);
        Assert.True(bottom <= viewBox[1] + viewBox[3]);
    }

    [Fact]
    public void AddsPreviouslyDeclaredNodesToSubgraphsAndAllowsWhitespaceBeforeTheTitle()
    {
        const string source = "flowchart LR\nA --> B\nsubgraph group [\"Group\"]\nA --> C\nend";

        var diagram = StaticCode.Diagrams.Flowchart.FlowchartParser.Parse(source);
        var root = XElement.Parse(MermaidRenderer.RenderSvg(source));
        var group = root.Descendants().Single(element => element.Attribute("class")?.Value == "diagram-subgraph-title");

        Assert.Equal(new[] { "A", "C" }, Assert.Single(diagram.Groups).NodeIds);
        Assert.Equal("Group", group.Value);
        Assert.Contains("diagram-subgraph", root.ToString());
    }

    [Fact]
    public void RendersSubgraphsContainingOnlyPreviouslyDeclaredNodes()
    {
        const string source = "flowchart LR\nA --> B\nsubgraph S\nA\nB\nend";

        var diagram = StaticCode.Diagrams.Flowchart.FlowchartParser.Parse(source);
        var root = XElement.Parse(MermaidRenderer.RenderSvg(source));

        Assert.Equal(new[] { "A", "B" }, Assert.Single(diagram.Groups).NodeIds);
        Assert.Contains(root.Descendants(), element => element.Attribute("class")?.Value == "diagram-subgraph");
    }

    [Fact]
    public void PreservesNamespacedIdentifiersInsideLabelsAndRendersWikiNodeShapes()
    {
        const string source = "flowchart LR\ntarget:App(\"target:App\") --> product:Lib[[\"product:Lib\"]]\nproduct:Lib --> Dependency{{\"Dependency\"}}";

        var svg = MermaidRenderer.RenderSvg(source);

        Assert.Contains("target:App", svg);
        Assert.Contains("product:Lib", svg);
        Assert.Contains("diagram-node-subroutine", svg);
        Assert.Contains("diagram-node-hexagon", svg);
    }

    [Fact]
    public void RejectsMermaidTypesNotImplementedByMdu()
    {
        const string source = "pie\n\"Dogs\" : 1";
        Assert.Throws<DiagramSyntaxException>(() => MermaidRenderer.RenderSvg(source));
        Assert.False(MermaidRenderer.TryRenderSvg(source, out var svg));
        Assert.Null(svg);
    }

    [Fact]
    public void RendersTheSameOutputEveryTime()
    {
        Assert.Equal(MermaidRenderer.RenderSvg(Sample), MermaidRenderer.RenderSvg(Sample));
    }
}
