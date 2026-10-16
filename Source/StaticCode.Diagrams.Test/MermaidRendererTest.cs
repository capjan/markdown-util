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

    [Theory]
    [InlineData("sequenceDiagram\nA->>B: Hi")]
    [InlineData("flowchart TD\nsubgraph One\nA\nend")]
    public void RejectsUnsupportedDiagrams(string source)
    {
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
