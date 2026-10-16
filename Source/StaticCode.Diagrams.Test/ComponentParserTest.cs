using StaticCode.Diagrams.Component;

namespace StaticCode.Diagrams.Test;

public class ComponentParserTest
{
    [Theory]
    [InlineData("[First component]", "First component", "First component", ElementKind.Component)]
    [InlineData("[Another component] as Comp2", "Comp2", "Another component", ElementKind.Component)]
    [InlineData("component Comp3", "Comp3", "Comp3", ElementKind.Component)]
    [InlineData("component [Last\\ncomponent] as Comp4", "Comp4", "Last\ncomponent", ElementKind.Component)]
    [InlineData("component Comp5 as \"Shown name\"", "Comp5", "Shown name", ElementKind.Component)]
    [InlineData("() \"First Interface\"", "First Interface", "First Interface", ElementKind.Interface)]
    [InlineData("() \"Another interface\" as Interf2", "Interf2", "Another interface", ElementKind.Interface)]
    [InlineData("interface Interf3", "Interf3", "Interf3", ElementKind.Interface)]
    [InlineData("interface \"Last\\ninterface\" as Interf4", "Interf4", "Last\ninterface", ElementKind.Interface)]
    [InlineData("database \"MySql\" as DB", "DB", "MySql", ElementKind.Database)]
    [InlineData("node Server", "Server", "Server", ElementKind.Node)]
    [InlineData("queue Events", "Events", "Events", ElementKind.Queue)]
    [InlineData("artifact \"app.jar\" as Jar", "Jar", "app.jar", ElementKind.Artifact)]
    public void ParsesElementDeclarations(string statement, string id, string label, ElementKind kind)
    {
        var diagram = ComponentParser.Parse($"@startuml\n{statement}\n@enduml");

        var element = Assert.Single(diagram.Elements);
        Assert.Equal(id, element.Id);
        Assert.Equal(label, element.Label);
        Assert.Equal(kind, element.Kind);
    }

    [Theory]
    [InlineData("[A] --> [B]", LinkDirection.Down, 1, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] ---> [B]", LinkDirection.Down, 2, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] -> [B]", LinkDirection.Right, 1, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] - [B]", LinkDirection.Right, 1, LinkLine.Solid, LinkHead.None, LinkHead.None)]
    [InlineData("[A] ..> [B]", LinkDirection.Down, 1, LinkLine.Dotted, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] .> [B]", LinkDirection.Right, 1, LinkLine.Dotted, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] ==> [B]", LinkDirection.Down, 1, LinkLine.Bold, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] <--> [B]", LinkDirection.Down, 1, LinkLine.Solid, LinkHead.Arrow, LinkHead.Arrow)]
    [InlineData("[A] <|-- [B]", LinkDirection.Down, 1, LinkLine.Solid, LinkHead.Triangle, LinkHead.None)]
    [InlineData("[A] ..|> [B]", LinkDirection.Down, 1, LinkLine.Dotted, LinkHead.None, LinkHead.Triangle)]
    [InlineData("[A] -up-> [B]", LinkDirection.Up, 1, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] -u-> [B]", LinkDirection.Up, 1, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] -left-> [B]", LinkDirection.Left, 1, LinkLine.Solid, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] .right.> [B]", LinkDirection.Right, 1, LinkLine.Dotted, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] -[dashed]-> [B]", LinkDirection.Down, 1, LinkLine.Dotted, LinkHead.None, LinkHead.Arrow)]
    [InlineData("[A] -[#red,bold]-> [B]", LinkDirection.Down, 1, LinkLine.Bold, LinkHead.None, LinkHead.Arrow)]
    public void ParsesLinks(string statement, LinkDirection direction, int length, LinkLine line, LinkHead fromHead, LinkHead toHead)
    {
        var link = Assert.Single(ComponentParser.Parse(statement).Links);

        Assert.Equal(new ComponentLink("A", "B", null, line, fromHead, toHead, direction, length), link);
    }

    [Fact]
    public void ParsesLabelsHiddenLinksAndImplicitInterfaces()
    {
        var diagram = ComponentParser.Parse("""
            DataAccess - [First Component]
            [First Component] ..> HTTP : use\nit
            [First Component] -[hidden]- [Second]
            """);

        Assert.Equal(ElementKind.Interface, diagram.Elements.Single(element => element.Id == "DataAccess").Kind);
        Assert.Equal(ElementKind.Interface, diagram.Elements.Single(element => element.Id == "HTTP").Kind);
        Assert.Equal("use\nit", diagram.Links[1].Label);
        Assert.True(diagram.Links[2].Hidden);
    }

    [Fact]
    public void TurnsExplicitDirectionsInLeftToRightDiagrams()
    {
        var diagram = ComponentParser.Parse("left to right direction\n[A] -down-> [B]\n[A] -right-> [C]\n[A] --> [D]\n[A] -> [E]");

        Assert.True(diagram.LeftToRight);
        Assert.Equal(
            new[] { LinkDirection.Right, LinkDirection.Down, LinkDirection.Down, LinkDirection.Right },
            diagram.Links.Select(link => link.Direction));
    }

    [Fact]
    public void ParsesNestedContainers()
    {
        var diagram = ComponentParser.Parse("""
            package "Some Group" {
              HTTP - [First Component]
              node Server {
                [Inner]
              }
            }
            cloud {
              [Example]
            }
            together {
              [Loose]
            }
            """);

        var group = diagram.Elements.Single(element => element.Id == "Some Group");
        Assert.True(group.IsContainer);
        Assert.Equal("Some Group", diagram.Elements.Single(element => element.Id == "HTTP").Parent);
        Assert.Equal("Server", diagram.Elements.Single(element => element.Id == "Inner").Parent);
        Assert.Equal("Some Group", diagram.Elements.Single(element => element.Id == "Server").Parent);
        var cloud = diagram.Elements.Single(element => element.Kind == ElementKind.Cloud);
        Assert.True(cloud.IsContainer);
        Assert.Equal(cloud.Id, diagram.Elements.Single(element => element.Id == "Example").Parent);
        Assert.Null(diagram.Elements.Single(element => element.Id == "Loose").Parent);
    }

    [Fact]
    public void ParsesNotesStereotypesTitlesAndComments()
    {
        var diagram = ComponentParser.Parse("""
            @startuml
            title Shop
            skinparam componentStyle {
              BackgroundColor red
            }
            hide stereotype
            ' a comment
            /' a block
               comment '/
            [Web Shop] <<frontend>> as Shop
            note left of Shop : Served by CDN
            note right of [Web Shop]
              Line one
              Line two
            end note
            note "Floating" as N1
            N1 .. Shop
            @enduml
            """);

        Assert.Equal("Shop", diagram.Title);
        var shop = diagram.Elements.Single(element => element.Id == "Shop");
        Assert.Equal("frontend", shop.Stereotype);
        var notes = diagram.Elements.Where(element => element.Kind == ElementKind.Note).ToList();
        Assert.Equal(new[] { "Served by CDN", "Line one\nLine two", "Floating" }, notes.Select(note => note.Label));
        Assert.Equal(new ComponentLink(notes[0].Id, "Shop", null, LinkLine.Dotted, LinkHead.None, LinkHead.None, LinkDirection.Right), diagram.Links[0]);
        Assert.Equal(new ComponentLink("Shop", notes[1].Id, null, LinkLine.Dotted, LinkHead.None, LinkHead.None, LinkDirection.Right), diagram.Links[1]);
    }

    [Theory]
    [InlineData("Alice -> Bob: Hello")]
    [InlineData("class Foo")]
    [InlineData("[A] --> [A]")]
    [InlineData("[A] --* [B]")]
    [InlineData("package P {\n[A]")]
    [InlineData("[A]\n}")]
    [InlineData("note right of [A]\ntext")]
    [InlineData("usecase (Login)")]
    public void RejectsUnsupportedSyntax(string source)
    {
        Assert.Throws<DiagramSyntaxException>(() => ComponentParser.Parse(source));
    }
}
