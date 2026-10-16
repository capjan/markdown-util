using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StaticCode.Diagrams.Test;

public class ComponentRendererTest
{
    private const string Sample = """
        @startuml
        title Order service
        actor Customer
        package "Shop" {
          [Web Shop] <<frontend>> as Web
          component [Order Service] as Orders
        }
        node "Data" {
          database "Orders DB" as DB
          queue "Events" as Bus
        }
        cloud {
          [CDN]
        }
        Customer --> Web : browses
        Web --> Orders : REST
        Orders --> DB
        Orders -right-> Bus : publishes
        Orders ..|> IOrders
        [CDN] -up-> Web
        Web -[hidden]- [CDN]
        note right of DB : PostgreSQL
        artifact "app.jar" as Jar
        file "config.yml" as Config
        storage "S3" as Store
        folder "Logs" as Logs
        frame "Frame" as Frame
        Jar .> Config
        @enduml
        """;

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [Fact]
    public void RendersWellFormedThemeableSvg()
    {
        var svg = DiagramRenderer.RenderPlantUmlSvg(Sample);

        var root = XElement.Parse(svg);
        Assert.Equal("diagram-component", root.Attribute("class")?.Value);
        Assert.Equal("Order service", root.Attribute("aria-label")?.Value);
        Assert.DoesNotMatch(new Regex(@"\b(fill|stroke|style)="), svg);
        Assert.DoesNotContain("#", svg);
    }

    [Fact]
    public void DrawsEveryElementWithItsShape()
    {
        var svg = DiagramRenderer.RenderPlantUmlSvg(Sample);

        foreach (var cssClass in new[]
                 {
                     "diagram-component-component", "diagram-component-interface", "diagram-component-actor",
                     "diagram-component-database", "diagram-component-queue", "diagram-component-note",
                     "diagram-component-artifact", "diagram-component-file", "diagram-component-storage",
                     "diagram-component-folder", "diagram-component-frame", "diagram-container-package",
                     "diagram-container-node", "diagram-container-cloud", "diagram-node-icon", "diagram-note-shape",
                     "diagram-marker-hollow", "diagram-edge-dotted", "diagram-component-title"
                 })
            Assert.Contains(cssClass, svg);
        Assert.Contains("«frontend»", svg);
    }

    [Fact]
    public void SkipsHiddenLinks()
    {
        var visible = Regex.Matches(DiagramRenderer.RenderPlantUmlSvg("[A] --> [B]\n[A] -[hidden]-> [C]"), "class=\"diagram-edge\"").Count;

        Assert.Equal(1, visible);
    }

    [Fact]
    public void PlacesContainerContentInsideTheContainer()
    {
        var root = XElement.Parse(DiagramRenderer.RenderPlantUmlSvg("rectangle P {\n[A] --> [B]\n}\n[C] --> [A]"));

        var container = Bounds(root.Descendants(Svg + "g").Single(group => Class(group).Contains("diagram-container-rectangle")).Element(Svg + "rect")!);
        foreach (var name in new[] { "A", "B" })
        {
            var rect = Bounds(ElementShape(root, name));
            Assert.True(rect.Left > container.Left && rect.Right < container.Right && rect.Top > container.Top && rect.Bottom < container.Bottom, name);
        }

        var outside = Bounds(ElementShape(root, "C"));
        Assert.True(outside.Bottom < container.Top || outside.Right < container.Left || outside.Left > container.Right);
    }

    [Fact]
    public void KeepsFlatLinksInOneRow()
    {
        var root = XElement.Parse(DiagramRenderer.RenderPlantUmlSvg("[Left] -> [Right]\n[Top] -left-> [Bottom]"));

        Assert.Equal(Bounds(ElementShape(root, "Left")).Top, Bounds(ElementShape(root, "Right")).Top, 1);
        Assert.True(Bounds(ElementShape(root, "Left")).Right < Bounds(ElementShape(root, "Right")).Left);
        Assert.True(Bounds(ElementShape(root, "Bottom")).Right < Bounds(ElementShape(root, "Top")).Left);
    }

    [Fact]
    public void PlacesUpLinksAboveTheirSource()
    {
        var root = XElement.Parse(DiagramRenderer.RenderPlantUmlSvg("[Source] -up-> [Target]"));

        Assert.True(Bounds(ElementShape(root, "Target")).Bottom < Bounds(ElementShape(root, "Source")).Top);
    }

    [Theory]
    [InlineData("plantuml")]
    [InlineData("puml")]
    public void RoutesPlantUmlFences(string language)
    {
        Assert.True(DiagramRenderer.TryRenderSvg(language, "@startuml\n[A] --> [B]\n@enduml", out var svg));
        Assert.Contains("diagram-component", svg);
        Assert.False(DiagramRenderer.TryRenderSvg(language, "@startuml\nAlice -> Bob: Hi\n@enduml", out _));
    }

    [Fact]
    public void RejectsLinksIntoTheirOwnContainer()
    {
        Assert.Throws<DiagramSyntaxException>(() => DiagramRenderer.RenderPlantUmlSvg("package P {\n[A]\n}\nP --> [A]"));
    }

    private static string Class(XElement element) => element.Attribute("class")?.Value ?? string.Empty;

    private static XElement ElementShape(XElement root, string text) =>
        root.Descendants(Svg + "g")
            .Where(group => Class(group).Contains("diagram-component-"))
            .Single(group => group.Element(Svg + "text")?.Value == text)
            .Element(Svg + "rect")!;

    private static Box Bounds(XElement shape)
    {
        double Value(string name) => double.Parse(shape.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        return new Box(Value("x"), Value("y"), Value("x") + Value("width"), Value("y") + Value("height"));
    }

    private sealed record Box(double Left, double Top, double Right, double Bottom);
}
