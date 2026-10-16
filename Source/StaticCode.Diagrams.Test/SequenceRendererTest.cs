using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StaticCode.Diagrams.Test;

public class SequenceRendererTest
{
    private const string Sample = """
        sequenceDiagram
            title Checkout
            autonumber
            actor C as Customer
            participant S as Shop
            participant P as Payment provider
            C->>+S: Place order
            alt Card accepted
                S->>P: Charge card with a rather long description
                P-->>S: Approved
            else Declined
                S--xC: Declined
            end
            loop Every minute
                S->>S: Check queue
            end
            Note over C,P: All calls use TLS
            Note right of P: Audited
            rect rgb(0, 0, 0)
                S-)C: Receipt
            end
            S-->>-C: Done
            C<<->>S: Keep-alive
        """;

    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    [Fact]
    public void RendersWellFormedSvg()
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg(Sample));

        Assert.Equal("svg", root.Name.LocalName);
        Assert.Equal("diagram-sequence", root.Attribute("class")?.Value);
        Assert.Equal("Checkout", root.Attribute("aria-label")?.Value);
    }

    [Fact]
    public void LeavesAllColorsToTheStylesheet()
    {
        var svg = MermaidRenderer.RenderSvg(Sample);

        Assert.DoesNotMatch(new Regex(@"\b(fill|stroke|style)="), svg);
        Assert.DoesNotContain("#", svg);
        Assert.DoesNotContain("rgb", svg);
    }

    [Fact]
    public void WritesThemeClassesForEveryElement()
    {
        var svg = MermaidRenderer.RenderSvg(Sample);

        foreach (var cssClass in new[]
                 {
                     "diagram-sequence-lifeline", "diagram-sequence-activation", "diagram-sequence-participant",
                     "diagram-sequence-actor-head", "diagram-sequence-note-shape", "diagram-sequence-frame",
                     "diagram-sequence-frame-tab", "diagram-sequence-frame-divider", "diagram-sequence-highlight",
                     "diagram-sequence-number", "diagram-sequence-title", "diagram-marker-open", "diagram-marker-cross",
                     "diagram-edge-dotted"
                 })
            Assert.Contains(cssClass, svg);
    }

    [Fact]
    public void DrawsEveryParticipantAtTheTopAndBottom()
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg(Sample));

        Assert.Equal(4, Descendants(root, "diagram-sequence-participant").Count());
        Assert.Equal(2, Descendants(root, "diagram-sequence-actor").Count());
        Assert.Equal(3, Descendants(root, "diagram-sequence-lifeline").Count());
    }

    [Fact]
    public void NumbersMessagesInOrder()
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg(Sample));

        var numbers = Descendants(root, "diagram-sequence-number").Select(group => group.Value).ToList();
        Assert.Equal(Enumerable.Range(1, 8).Select(number => number.ToString(CultureInfo.InvariantCulture)), numbers);
    }

    [Fact]
    public void SpacesLifelinesSoThatMessageTextFits()
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg("sequenceDiagram\nA->>B: A considerably longer message"));

        var lifelines = Descendants(root, "diagram-sequence-lifeline")
            .Select(line => double.Parse(Regex.Match(line.Attribute("d")!.Value, @"^M(?<x>[-\d.]+)").Groups["x"].Value, CultureInfo.InvariantCulture))
            .ToList();
        Assert.True(lifelines[1] - lifelines[0] > 200);
    }

    [Fact]
    public void ClosesBlocksAroundTheirMessages()
    {
        var root = XElement.Parse(MermaidRenderer.RenderSvg(Sample));

        var frames = Descendants(root, "diagram-sequence-frame").ToList();
        Assert.Equal(2, frames.Count);
        Assert.All(frames, frame => Assert.True(double.Parse(frame.Attribute("height")!.Value, CultureInfo.InvariantCulture) > 30));
    }

    [Fact]
    public void RendersTheSameOutputEveryTime()
    {
        Assert.Equal(MermaidRenderer.RenderSvg(Sample), MermaidRenderer.RenderSvg(Sample));
    }

    private static IEnumerable<XElement> Descendants(XElement root, string cssClass) =>
        root.Descendants().Where(element => (element.Attribute("class")?.Value ?? string.Empty).Split(' ').Contains(cssClass));
}
