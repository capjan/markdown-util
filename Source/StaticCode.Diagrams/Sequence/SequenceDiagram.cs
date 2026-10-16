namespace StaticCode.Diagrams.Sequence;

public enum ParticipantKind
{
    Participant,
    Actor
}

public enum MessageLine
{
    Solid,
    Dotted
}

public enum MessageHead
{
    None,
    Arrow,
    Open,
    Cross
}

public enum NotePlacement
{
    LeftOf,
    RightOf,
    Over
}

public enum BlockKind
{
    Loop,
    Alt,
    Opt,
    Par,
    Critical,
    Break,
    Rect
}

public sealed class SequenceParticipant
{
    public SequenceParticipant(string id)
    {
        Id = id;
        Label = id;
    }

    public string Id { get; }
    public string Label { get; set; }
    public ParticipantKind Kind { get; set; } = ParticipantKind.Participant;
}

public abstract record SequenceEvent;

/// <param name="ActivateTarget">Mermaid's <c>+</c> shortcut: the target becomes active with this message.</param>
/// <param name="DeactivateSource">Mermaid's <c>-</c> shortcut: the source stops being active with this message.</param>
public sealed record SequenceMessage(
    string From,
    string To,
    string? Text,
    MessageLine Line,
    MessageHead Head,
    bool Bidirectional = false,
    bool ActivateTarget = false,
    bool DeactivateSource = false) : SequenceEvent;

public sealed record SequenceNote(NotePlacement Placement, string First, string? Second, string Text) : SequenceEvent;

public sealed record SequenceActivation(string Participant, bool Active) : SequenceEvent;

public sealed record SequenceBlockStart(BlockKind Kind, string? Label) : SequenceEvent;

/// <summary>Starts another section of the open block: <c>else</c>, <c>and</c>, or <c>option</c>.</summary>
public sealed record SequenceBlockSection(string? Label) : SequenceEvent;

public sealed record SequenceBlockEnd : SequenceEvent;

/// <summary>Numbers the following messages, starting at <paramref name="Start"/>; <paramref name="Enabled"/> is false for <c>autonumber off</c>.</summary>
public sealed record SequenceAutonumber(bool Enabled, int Start = 1, int Step = 1) : SequenceEvent;

public sealed class SequenceDiagram
{
    public SequenceDiagram(string? title, string? accessibleTitle, IReadOnlyList<SequenceParticipant> participants, IReadOnlyList<SequenceEvent> events)
    {
        Title = title;
        AccessibleTitle = accessibleTitle;
        Participants = participants;
        Events = events;
    }

    /// <summary>Visible title from Mermaid's <c>title</c> statement.</summary>
    public string? Title { get; }

    /// <summary>Accessible title from Mermaid's <c>accTitle</c> statement.</summary>
    public string? AccessibleTitle { get; }

    public IReadOnlyList<SequenceParticipant> Participants { get; }
    public IReadOnlyList<SequenceEvent> Events { get; }
}
