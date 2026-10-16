using StaticCode.Diagrams.Sequence;

namespace StaticCode.Diagrams.Test;

public class SequenceParserTest
{
    [Fact]
    public void ParsesParticipantsInOrderOfAppearance()
    {
        var diagram = SequenceParser.Parse("""
            sequenceDiagram
                actor U as User
                participant API
                U->>DB: Query
                participant DB as Database<br>primary
            """);

        Assert.Equal(new[] { "U", "API", "DB" }, diagram.Participants.Select(participant => participant.Id));
        Assert.Equal(new[] { "User", "API", "Database\nprimary" }, diagram.Participants.Select(participant => participant.Label));
        Assert.Equal(ParticipantKind.Actor, diagram.Participants[0].Kind);
        Assert.Equal(ParticipantKind.Participant, diagram.Participants[2].Kind);
    }

    [Theory]
    [InlineData("->", MessageLine.Solid, MessageHead.None, false)]
    [InlineData("-->", MessageLine.Dotted, MessageHead.None, false)]
    [InlineData("->>", MessageLine.Solid, MessageHead.Arrow, false)]
    [InlineData("-->>", MessageLine.Dotted, MessageHead.Arrow, false)]
    [InlineData("-x", MessageLine.Solid, MessageHead.Cross, false)]
    [InlineData("--x", MessageLine.Dotted, MessageHead.Cross, false)]
    [InlineData("-)", MessageLine.Solid, MessageHead.Open, false)]
    [InlineData("--)", MessageLine.Dotted, MessageHead.Open, false)]
    [InlineData("<<->>", MessageLine.Solid, MessageHead.Arrow, true)]
    [InlineData("<<-->>", MessageLine.Dotted, MessageHead.Arrow, true)]
    public void ParsesMessageArrows(string arrow, MessageLine line, MessageHead head, bool bidirectional)
    {
        var diagram = SequenceParser.Parse($"sequenceDiagram\nAlice{arrow}Bob: Hello there");

        Assert.Equal(
            new SequenceMessage("Alice", "Bob", "Hello there", line, head, bidirectional),
            Assert.Single(diagram.Events));
    }

    [Fact]
    public void ParsesMessagesWithoutTextAndWithSpaces()
    {
        var diagram = SequenceParser.Parse("sequenceDiagram\n  Web App ->> API\n");

        var message = Assert.IsType<SequenceMessage>(Assert.Single(diagram.Events));
        Assert.Equal("Web App", message.From);
        Assert.Null(message.Text);
    }

    [Fact]
    public void ParsesActivationShortcutsAndStatements()
    {
        var diagram = SequenceParser.Parse("""
            sequenceDiagram
                A->>+B: Request
                B-->>-A: Response
                activate A
                deactivate A
            """);

        var request = Assert.IsType<SequenceMessage>(diagram.Events[0]);
        var response = Assert.IsType<SequenceMessage>(diagram.Events[1]);
        Assert.True(request.ActivateTarget);
        Assert.True(response.DeactivateSource);
        Assert.Equal(new SequenceActivation("A", true), diagram.Events[2]);
        Assert.Equal(new SequenceActivation("A", false), diagram.Events[3]);
    }

    [Theory]
    [InlineData("Note right of A: Text", NotePlacement.RightOf, "A", null)]
    [InlineData("note left of A : Text", NotePlacement.LeftOf, "A", null)]
    [InlineData("Note over A: Text", NotePlacement.Over, "A", null)]
    [InlineData("Note over A,B: Text", NotePlacement.Over, "A", "B")]
    [InlineData("Note over A, A: Text", NotePlacement.Over, "A", null)]
    public void ParsesNotes(string statement, NotePlacement placement, string first, string? second)
    {
        var diagram = SequenceParser.Parse($"sequenceDiagram\nparticipant A\nparticipant B\n{statement}");

        Assert.Equal(new SequenceNote(placement, first, second, "Text"), Assert.Single(diagram.Events));
    }

    [Fact]
    public void ParsesBlocks()
    {
        var diagram = SequenceParser.Parse("""
            sequenceDiagram
                alt Success
                    A->>B: Ok
                else Failure
                    A->>B: Error
                end
                loop Every minute
                    par
                        A->>B: One
                    and Second
                        A->>C: Two
                    end
                end
                critical Connect
                    A->>B: Open
                option Timeout
                    A->>A: Retry
                end
                rect rgb(0, 0, 255)
                    opt
                        A-)B: Fire
                    end
                end
                break Stop
                    A-xB: Abort
                end
            """);

        var structure = diagram.Events
            .Where(item => item is not SequenceMessage)
            .Select(item => item switch
            {
                SequenceBlockStart start => $"{start.Kind}:{start.Label}",
                SequenceBlockSection section => $"section:{section.Label}",
                _ => "end"
            });
        Assert.Equal(
            new[]
            {
                "Alt:Success", "section:Failure", "end",
                "Loop:Every minute", "Par:", "section:Second", "end", "end",
                "Critical:Connect", "section:Timeout", "end",
                "Rect:", "Opt:", "end", "end",
                "Break:Stop", "end"
            },
            structure);
    }

    [Fact]
    public void ParsesTitlesAutonumberCommentsAndSemicolons()
    {
        var diagram = SequenceParser.Parse("""
            %% comment
            sequenceDiagram
                title: Login
                accTitle: Login flow
                accDescr: Ignored description
                autonumber 10 5
                A->>B: Hi;
                autonumber off
            """);

        Assert.Equal("Login", diagram.Title);
        Assert.Equal("Login flow", diagram.AccessibleTitle);
        Assert.Equal(new SequenceAutonumber(true, 10, 5), diagram.Events[0]);
        Assert.Equal("Hi", Assert.IsType<SequenceMessage>(diagram.Events[1]).Text);
        Assert.Equal(new SequenceAutonumber(false), diagram.Events[2]);
    }

    [Theory]
    [InlineData("box Group\nparticipant A\nend", 2)]
    [InlineData("create participant A", 2)]
    [InlineData("A->>B: Hi\ndestroy B", 3)]
    [InlineData("A->>B: Hi\nelse Nope", 3)]
    [InlineData("A->>B: Hi\nend", 3)]
    [InlineData("A->>B: Hi\nloop Forever\nA->>B: Again", 4)]
    [InlineData("A->>B: Hi\ndeactivate A", 3)]
    [InlineData("A->>-B: Hi", 2)]
    [InlineData("Note left of A,B: Text", 2)]
    [InlineData("participant A@{ \"type\": \"database\" }", 2)]
    [InlineData("A=>B: Hi", 2)]
    public void RejectsUnsupportedSyntax(string statements, int line)
    {
        var exception = Assert.Throws<DiagramSyntaxException>(() => SequenceParser.Parse($"sequenceDiagram\n{statements}"));

        Assert.Equal(line, exception.Line);
    }

    [Fact]
    public void RequiresAParticipant()
    {
        Assert.Throws<DiagramSyntaxException>(() => SequenceParser.Parse("sequenceDiagram\n%% nothing"));
    }

    [Theory]
    [InlineData("sequenceDiagram\nA->>B: Hi", true)]
    [InlineData("%% comment\n\n  sequenceDiagram\n", true)]
    [InlineData("flowchart TD\nA --> B", false)]
    [InlineData("sequenceDiagram-v2", false)]
    public void DetectsSequenceDiagrams(string source, bool expected)
    {
        Assert.Equal(expected, SequenceParser.CanParse(source));
    }
}
