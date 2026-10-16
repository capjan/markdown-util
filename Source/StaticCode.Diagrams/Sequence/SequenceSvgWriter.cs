using System.Text;
using StaticCode.Diagrams.Layout;
using StaticCode.Diagrams.Svg;
using StaticCode.Diagrams.Text;
using static StaticCode.Diagrams.Svg.SvgText;

namespace StaticCode.Diagrams.Sequence;

/// <summary>
/// Lays out and writes a sequence diagram as inline SVG. Lifelines are spaced so that every message text, note,
/// and self-message fits between them; events then stack from top to bottom. Like flowcharts, the SVG contains
/// no colors, only <c>diagram-*</c> classes for the page theme.
/// </summary>
internal sealed class SequenceSvgWriter
{
    private const double ParticipantPaddingX = 16;
    private const double ParticipantPaddingY = 10;
    private const double MinParticipantWidth = 80;
    private const double ParticipantGap = 28;
    private const double MaxParticipantTextWidth = 160;
    private const double MaxMessageTextWidth = 260;
    private const double MaxNoteTextWidth = 200;
    private const double StickFigureHeight = 38;
    private const double MessagePadding = 14;
    private const double ActivationWidth = 10;
    private const double SelfLoopWidth = 32;
    private const double SelfLoopHeight = 22;
    private const double NotePaddingX = 10;
    private const double NotePaddingY = 6;
    private const double NoteGap = 8;
    private const double NoteOverhang = 16;
    private const double FramePadding = 12;
    private const double FrameHeaderHeight = TextMetrics.LineHeight + 6;
    private const double RowGap = 14;
    private const double TitleHeight = TextMetrics.LineHeight + 14;
    private const double CanvasPadding = 4;

    private readonly SequenceDiagram _diagram;
    private readonly Dictionary<string, int> _index;
    private readonly ParticipantBox[] _boxes;
    private readonly double[] _x;
    private readonly Stack<double>[] _active;
    private readonly List<(int Participant, int Level, double Start, double End)> _activations = new();
    private readonly List<Frame> _frames = new();
    private readonly Stack<Frame> _openFrames = new();
    private readonly StringBuilder _messages = new();
    private readonly StringBuilder _notes = new();
    private double _minX = double.PositiveInfinity;
    private double _maxX = double.NegativeInfinity;
    private double _y;
    private double? _lastArrowY;
    private int? _number;
    private int _numberStep = 1;

    private SequenceSvgWriter(SequenceDiagram diagram)
    {
        _diagram = diagram;
        _index = diagram.Participants.Select((participant, index) => (participant.Id, index)).ToDictionary(item => item.Id, item => item.index);
        _boxes = diagram.Participants.Select(CreateParticipantBox).ToArray();
        _x = PlaceLifelines();
        _active = _boxes.Select(_ => new Stack<double>()).ToArray();
    }

    public static string Write(SequenceDiagram diagram) => new SequenceSvgWriter(diagram).Write();

    private string Write()
    {
        var headerTop = _diagram.Title == null ? 0 : TitleHeight;
        var headerHeight = _boxes.Max(box => box.Height);
        var headerBottom = headerTop + headerHeight;
        _y = headerBottom + 16;

        foreach (var sequenceEvent in _diagram.Events)
        {
            switch (sequenceEvent)
            {
                case SequenceMessage message:
                    WriteMessage(message);
                    break;
                case SequenceNote note:
                    WriteNote(note);
                    break;
                case SequenceActivation activation when activation.Active:
                    _active[_index[activation.Participant]].Push(_lastArrowY ?? _y);
                    break;
                case SequenceActivation activation:
                    Deactivate(_index[activation.Participant], _lastArrowY ?? _y);
                    break;
                case SequenceAutonumber autonumber:
                    _number = autonumber.Enabled ? autonumber.Start : null;
                    _numberStep = autonumber.Step;
                    break;
                case SequenceBlockStart start:
                    _openFrames.Push(new Frame(start.Kind, start.Label, _y, _openFrames.Count));
                    _y += start.Kind == BlockKind.Rect ? 8 : FrameHeaderHeight + 8;
                    break;
                case SequenceBlockSection section:
                    _openFrames.Peek().Sections.Add((_y, section.Label));
                    _y += section.Label == null ? 12 : TextMetrics.LineHeight + 12;
                    break;
                case SequenceBlockEnd:
                    CloseFrame();
                    break;
            }
        }

        var footerTop = _y + 6;
        for (var participant = 0; participant < _active.Length; participant++)
            while (_active[participant].Count > 0)
                Deactivate(participant, footerTop - 6);

        var participants = new StringBuilder();
        for (var index = 0; index < _boxes.Length; index++)
        {
            WriteParticipant(participants, index, headerBottom - _boxes[index].Height);
            WriteParticipant(participants, index, footerTop);
        }

        var title = new StringBuilder();
        if (_diagram.Title != null)
        {
            var lines = TextMetrics.Wrap(_diagram.Title, double.MaxValue);
            var center = (_x[0] + _x[^1]) / 2;
            var titleWidth = TextMetrics.Measure(lines).Width;
            Include(center - titleWidth / 2, center + titleWidth / 2);
            WriteText(title, lines, new Point(center, TitleHeight / 2 - 2), "diagram-sequence-title");
        }

        var left = _minX - CanvasPadding;
        var width = _maxX - _minX + 2 * CanvasPadding;
        var height = footerTop + headerHeight + 2 * CanvasPadding;
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram-sequence\"")
            .Append($" viewBox=\"{F(left)} {F(-CanvasPadding)} {F(width)} {F(height)}\"")
            .Append($" width=\"{F(width)}\" height=\"{F(height)}\"")
            .Append($" font-size=\"{F(TextMetrics.FontSize)}\" role=\"img\" aria-label=\"{Escape(_diagram.AccessibleTitle ?? _diagram.Title ?? "Sequence diagram")}\">\n");

        svg.Append("<g class=\"diagram-sequence-blocks\">\n");
        foreach (var frame in _frames.OrderBy(frame => frame.Depth)) WriteFrame(svg, frame);
        svg.Append("</g>\n<g class=\"diagram-sequence-lifelines\">\n");
        foreach (var x in _x)
            svg.Append($"<path class=\"diagram-sequence-lifeline\" d=\"M{F(x)} {F(headerBottom)} V{F(footerTop)}\"/>\n");
        svg.Append("</g>\n<g class=\"diagram-sequence-activations\">\n");
        foreach (var (participant, level, start, end) in _activations.OrderBy(activation => activation.Level))
        {
            var center = _x[participant] + (level - 1) * ActivationWidth / 2;
            svg.Append($"<rect class=\"diagram-sequence-activation\" x=\"{F(center - ActivationWidth / 2)}\" y=\"{F(start)}\"")
                .Append($" width=\"{F(ActivationWidth)}\" height=\"{F(Math.Max(end - start, 8))}\"/>\n");
        }

        svg.Append("</g>\n<g class=\"diagram-sequence-messages\">\n").Append(_messages)
            .Append("</g>\n<g class=\"diagram-sequence-notes\">\n").Append(_notes)
            .Append("</g>\n<g class=\"diagram-sequence-participants\">\n").Append(participants)
            .Append("</g>\n").Append(title)
            .Append("</svg>");
        return svg.ToString();
    }

    private static ParticipantBox CreateParticipantBox(SequenceParticipant participant)
    {
        var lines = TextMetrics.Wrap(participant.Label, MaxParticipantTextWidth);
        var (textWidth, textHeight) = TextMetrics.Measure(lines);
        return participant.Kind == ParticipantKind.Actor
            ? new ParticipantBox(participant.Kind, lines, Math.Max(textWidth + 8, 48), StickFigureHeight + 4 + textHeight)
            : new ParticipantBox(participant.Kind, lines, Math.Max(textWidth + 2 * ParticipantPaddingX, MinParticipantWidth), textHeight + 2 * ParticipantPaddingY);
    }

    /// <summary>
    /// Spaces the lifelines: neighboring boxes need room, and every message, note, and self-message requires a
    /// minimum distance between the lifelines it spans. Shorter spans are satisfied first; a longer span that is
    /// still too narrow spreads its missing width evenly over its gaps.
    /// </summary>
    private double[] PlaceLifelines()
    {
        var count = _boxes.Length;
        var gaps = new double[Math.Max(count - 1, 0)];
        for (var index = 0; index + 1 < count; index++)
            gaps[index] = _boxes[index].Width / 2 + _boxes[index + 1].Width / 2 + ParticipantGap;

        var numberWidth = _diagram.Events.Any(item => item is SequenceAutonumber { Enabled: true }) ? 18 : 0;
        var constraints = new List<(int Low, int High, double Distance)>();
        void Require(int low, int high, double distance)
        {
            if (low >= 0 && high < count && low < high) constraints.Add((low, high, distance));
        }

        foreach (var sequenceEvent in _diagram.Events)
        {
            switch (sequenceEvent)
            {
                case SequenceMessage message:
                {
                    var from = _index[message.From];
                    var to = _index[message.To];
                    var textWidth = message.Text == null ? 0 : TextMetrics.Measure(TextMetrics.Wrap(message.Text, MaxMessageTextWidth)).Width;
                    if (from == to)
                        Require(from, from + 1, Math.Max(SelfLoopWidth, textWidth + 6) + ActivationWidth + 20);
                    else
                        Require(Math.Min(from, to), Math.Max(from, to), textWidth + 2 * MessagePadding + ActivationWidth + numberWidth);
                    break;
                }
                case SequenceNote note:
                {
                    var width = CreateNoteBox(note.Text).Width;
                    var first = _index[note.First];
                    if (note.Second != null)
                    {
                        var second = _index[note.Second];
                        Require(Math.Min(first, second), Math.Max(first, second), width - 2 * NoteOverhang);
                    }
                    else if (note.Placement == NotePlacement.RightOf)
                    {
                        Require(first, first + 1, ActivationWidth / 2 + NoteGap + width + 16);
                    }
                    else if (note.Placement == NotePlacement.LeftOf)
                    {
                        Require(first - 1, first, ActivationWidth / 2 + NoteGap + width + 16);
                    }
                    else
                    {
                        Require(first - 1, first, width / 2 + 16);
                        Require(first, first + 1, width / 2 + 16);
                    }

                    break;
                }
            }
        }

        foreach (var (low, high, distance) in constraints.OrderBy(constraint => constraint.High - constraint.Low))
        {
            var missing = distance - gaps[low..high].Sum();
            if (missing <= 0) continue;
            for (var index = low; index < high; index++) gaps[index] += missing / (high - low);
        }

        var x = new double[count];
        for (var index = 1; index < count; index++) x[index] = x[index - 1] + gaps[index - 1];
        return x;
    }

    private void WriteMessage(SequenceMessage message)
    {
        var from = _index[message.From];
        var to = _index[message.To];
        var lines = message.Text == null ? Array.Empty<string>() : TextMetrics.Wrap(message.Text, MaxMessageTextWidth);
        var (textWidth, textHeight) = TextMetrics.Measure(lines);
        var textTop = _y;
        var arrowY = _y + textHeight + (textHeight > 0 ? 6 : 2);
        var head = message.Head switch
        {
            MessageHead.Arrow => MarkerKind.Arrow,
            MessageHead.Open => MarkerKind.OpenArrow,
            MessageHead.Cross => MarkerKind.Cross,
            _ => MarkerKind.None
        };
        var lineClass = message.Line == MessageLine.Dotted ? "diagram-edge diagram-edge-dotted" : "diagram-edge";

        if (from == to)
        {
            WriteSelfMessage(message, from, lines, textWidth, textTop, arrowY, head, lineClass);
            return;
        }

        if (message.ActivateTarget) _active[to].Push(arrowY);
        var rightwards = to > from;
        var direction = new Point(rightwards ? 1 : -1, 0);
        var fromX = EdgeX(from, rightwards);
        var toX = EdgeX(to, !rightwards);
        var lineStart = fromX + (message.Bidirectional ? direction.X * SvgMarkers.Inset(head) : 0);
        var lineEnd = toX - direction.X * SvgMarkers.Inset(head);

        _messages.Append($"<path class=\"{lineClass}\" d=\"M{F(lineStart)} {F(arrowY)} H{F(lineEnd)}\"/>");
        SvgMarkers.Write(_messages, head, new Point(toX, arrowY), direction);
        if (message.Bidirectional) SvgMarkers.Write(_messages, head, new Point(fromX, arrowY), direction * -1);
        var textCenter = (fromX + toX) / 2;
        if (lines.Count > 0) WriteText(_messages, lines, new Point(textCenter, textTop + textHeight / 2), "diagram-sequence-message-text");
        // Keep the number clear of the start arrowhead of a bidirectional message.
        WriteNumber(new Point(fromX + (message.Bidirectional ? direction.X * 20 : 0), arrowY));
        _messages.Append('\n');

        if (message.DeactivateSource) Deactivate(from, arrowY);
        Include(Math.Min(Math.Min(fromX, toX), textCenter - textWidth / 2), Math.Max(Math.Max(fromX, toX), textCenter + textWidth / 2));
        _lastArrowY = arrowY;
        _y = arrowY + RowGap;
    }

    private void WriteSelfMessage(
        SequenceMessage message, int participant, IReadOnlyList<string> lines, double textWidth, double textTop, double loopTop, MarkerKind head, string lineClass)
    {
        const double radius = 6;
        var x = EdgeX(participant, true);
        var right = x + SelfLoopWidth;
        var loopBottom = loopTop + SelfLoopHeight;
        var start = x + (message.Bidirectional ? SvgMarkers.Inset(head) : 0);
        var end = x + SvgMarkers.Inset(head);

        _messages.Append($"<path class=\"{lineClass}\" d=\"M{F(start)} {F(loopTop)} H{F(right - radius)} Q{F(right)} {F(loopTop)} {F(right)} {F(loopTop + radius)}")
            .Append($" V{F(loopBottom - radius)} Q{F(right)} {F(loopBottom)} {F(right - radius)} {F(loopBottom)} H{F(end)}\"/>");
        SvgMarkers.Write(_messages, head, new Point(x, loopBottom), new Point(-1, 0));
        if (message.Bidirectional) SvgMarkers.Write(_messages, head, new Point(x, loopTop), new Point(-1, 0));
        if (lines.Count > 0)
            WriteText(_messages, lines, new Point(x + 6, textTop + (loopTop - textTop - 6) / 2), "diagram-sequence-message-text", TextAlignment.Start);
        WriteNumber(new Point(x, loopTop));
        _messages.Append('\n');

        if (message.ActivateTarget) _active[participant].Push(loopBottom);
        if (message.DeactivateSource) Deactivate(participant, loopBottom);
        Include(x, Math.Max(right, x + 6 + textWidth));
        _lastArrowY = loopBottom;
        _y = loopBottom + RowGap;
    }

    private void WriteNumber(Point center)
    {
        if (_number is not { } number) return;
        var text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var radius = 8 + Math.Max(0, text.Length - 2) * 3;
        _messages.Append($"<g class=\"diagram-sequence-number\"><circle cx=\"{F(center.X)}\" cy=\"{F(center.Y)}\" r=\"{F(radius)}\"/>")
            .Append($"<text x=\"{F(center.X)}\" y=\"{F(center.Y + 11 * 0.35)}\" text-anchor=\"middle\" font-size=\"11\">{text}</text></g>");
        _number = number + _numberStep;
    }

    private void WriteNote(SequenceNote note)
    {
        var box = CreateNoteBox(note.Text);
        var first = _x[_index[note.First]];
        var width = box.Width;
        double left;
        if (note.Second != null)
        {
            var second = _x[_index[note.Second]];
            left = Math.Min(first, second) - NoteOverhang;
            var span = Math.Abs(second - first) + 2 * NoteOverhang;
            if (span < width) left -= (width - span) / 2;
            width = Math.Max(width, span);
        }
        else
        {
            left = note.Placement switch
            {
                NotePlacement.RightOf => first + ActivationWidth / 2 + NoteGap,
                NotePlacement.LeftOf => first - ActivationWidth / 2 - NoteGap - width,
                _ => first - width / 2
            };
        }

        _notes.Append($"<g class=\"diagram-sequence-note\"><rect class=\"diagram-sequence-note-shape\" x=\"{F(left)}\" y=\"{F(_y)}\"")
            .Append($" width=\"{F(width)}\" height=\"{F(box.Height)}\" rx=\"3\"/>");
        WriteText(_notes, box.Lines, new Point(left + width / 2, _y + box.Height / 2), "diagram-sequence-note-text");
        _notes.Append("</g>\n");

        Include(left, left + width);
        _y += box.Height + RowGap;
    }

    private static NoteBox CreateNoteBox(string text)
    {
        var lines = TextMetrics.Wrap(text, MaxNoteTextWidth);
        var (width, height) = TextMetrics.Measure(lines);
        return new NoteBox(lines, Math.Max(width + 2 * NotePaddingX, 48), height + 2 * NotePaddingY);
    }

    private void CloseFrame()
    {
        var frame = _openFrames.Pop();
        frame.Bottom = _y - RowGap + 10;
        _y = frame.Bottom + RowGap;

        if (double.IsInfinity(frame.MinX))
        {
            frame.MinX = _x[0];
            frame.MaxX = _x[^1];
        }

        frame.Left = frame.MinX - FramePadding;
        frame.Right = frame.MaxX + FramePadding;
        var minimumWidth = frame.Kind == BlockKind.Rect ? 0 : TabWidth(frame) + BracketWidth(frame.Label) + 12;
        foreach (var (_, label) in frame.Sections) minimumWidth = Math.Max(minimumWidth, BracketWidth(label) + 20);
        if (frame.Right - frame.Left < minimumWidth) frame.Right = frame.Left + minimumWidth;

        _frames.Add(frame);
        Include(frame.Left, frame.Right);
    }

    private static double TabWidth(Frame frame) => TextMetrics.Width(Keyword(frame.Kind)) + 16;

    private static double BracketWidth(string? label) => label == null ? 0 : TextMetrics.Width($"[{label}]") + 14;

    private static string Keyword(BlockKind kind) => kind.ToString().ToLowerInvariant();

    private static void WriteFrame(StringBuilder svg, Frame frame)
    {
        var width = frame.Right - frame.Left;
        var height = frame.Bottom - frame.Top;
        if (frame.Kind == BlockKind.Rect)
        {
            svg.Append($"<rect class=\"diagram-sequence-highlight\" x=\"{F(frame.Left)}\" y=\"{F(frame.Top)}\" width=\"{F(width)}\" height=\"{F(height)}\" rx=\"4\"/>\n");
            return;
        }

        var tabWidth = TabWidth(frame);
        var tabCenterY = frame.Top + FrameHeaderHeight / 2;
        svg.Append($"<g class=\"diagram-sequence-block diagram-sequence-block-{Keyword(frame.Kind)}\">")
            .Append($"<rect class=\"diagram-sequence-frame\" x=\"{F(frame.Left)}\" y=\"{F(frame.Top)}\" width=\"{F(width)}\" height=\"{F(height)}\" rx=\"4\"/>")
            .Append($"<path class=\"diagram-sequence-frame-tab\" d=\"M{F(frame.Left)} {F(frame.Top + 4)} Q{F(frame.Left)} {F(frame.Top)} {F(frame.Left + 4)} {F(frame.Top)}")
            .Append($" H{F(frame.Left + tabWidth + 6)} V{F(frame.Top + FrameHeaderHeight - 8)} L{F(frame.Left + tabWidth)} {F(frame.Top + FrameHeaderHeight)} H{F(frame.Left)} Z\"/>");
        WriteText(svg, new[] { Keyword(frame.Kind) }, new Point(frame.Left + 8, tabCenterY), "diagram-sequence-frame-keyword", TextAlignment.Start);
        if (frame.Label != null)
            WriteText(svg, new[] { $"[{frame.Label}]" }, new Point(frame.Left + tabWidth + 14, tabCenterY), "diagram-sequence-frame-label", TextAlignment.Start);

        foreach (var (y, label) in frame.Sections)
        {
            svg.Append($"<path class=\"diagram-sequence-frame-divider\" d=\"M{F(frame.Left)} {F(y)} H{F(frame.Right)}\"/>");
            if (label != null)
                WriteText(svg, new[] { $"[{label}]" }, new Point(frame.Left + 10, y + TextMetrics.LineHeight / 2 + 4), "diagram-sequence-frame-label", TextAlignment.Start);
        }

        svg.Append("</g>\n");
    }

    private void WriteParticipant(StringBuilder svg, int index, double top)
    {
        var box = _boxes[index];
        var x = _x[index];
        var textHeight = box.Lines.Count * TextMetrics.LineHeight;
        if (box.Kind == ParticipantKind.Actor)
        {
            svg.Append("<g class=\"diagram-sequence-actor\">")
                .Append($"<circle class=\"diagram-sequence-actor-head\" cx=\"{F(x)}\" cy=\"{F(top + 7)}\" r=\"7\"/>")
                .Append($"<path class=\"diagram-sequence-actor-body\" d=\"M{F(x)} {F(top + 14)} V{F(top + 27)} M{F(x - 11)} {F(top + 19)} H{F(x + 11)}")
                .Append($" M{F(x)} {F(top + 27)} L{F(x - 9)} {F(top + 37)} M{F(x)} {F(top + 27)} L{F(x + 9)} {F(top + 37)}\"/>");
            WriteText(svg, box.Lines, new Point(x, top + StickFigureHeight + 4 + textHeight / 2), "diagram-node-text");
        }
        else
        {
            svg.Append("<g class=\"diagram-node diagram-sequence-participant\">")
                .Append($"<rect class=\"diagram-node-shape\" x=\"{F(x - box.Width / 2)}\" y=\"{F(top)}\" width=\"{F(box.Width)}\" height=\"{F(box.Height)}\" rx=\"6\"/>");
            WriteText(svg, box.Lines, new Point(x, top + box.Height / 2), "diagram-node-text");
        }

        svg.Append("</g>\n");
        Include(x - box.Width / 2, x + box.Width / 2);
    }

    /// <summary>Where a message leaves or enters a lifeline: the edge of its innermost activation, if any.</summary>
    private double EdgeX(int participant, bool rightSide)
    {
        var depth = _active[participant].Count;
        if (depth == 0) return _x[participant];
        var center = _x[participant] + (depth - 1) * ActivationWidth / 2;
        return center + (rightSide ? ActivationWidth / 2 : -ActivationWidth / 2);
    }

    private void Deactivate(int participant, double y)
    {
        var level = _active[participant].Count;
        if (level == 0) return;
        _activations.Add((participant, level, _active[participant].Pop(), y));
    }

    /// <summary>Extends the canvas and the innermost open block to cover a horizontal range.</summary>
    private void Include(double minX, double maxX)
    {
        _minX = Math.Min(_minX, minX);
        _maxX = Math.Max(_maxX, maxX);
        if (_openFrames.Count == 0) return;
        var frame = _openFrames.Peek();
        frame.MinX = Math.Min(frame.MinX, minX);
        frame.MaxX = Math.Max(frame.MaxX, maxX);
    }

    private sealed record ParticipantBox(ParticipantKind Kind, IReadOnlyList<string> Lines, double Width, double Height);

    private sealed record NoteBox(IReadOnlyList<string> Lines, double Width, double Height);

    private sealed class Frame
    {
        public Frame(BlockKind kind, string? label, double top, int depth)
        {
            Kind = kind;
            Label = label;
            Top = top;
            Depth = depth;
        }

        public BlockKind Kind { get; }
        public string? Label { get; }
        public double Top { get; }
        public int Depth { get; }
        public double Bottom { get; set; }
        public double MinX { get; set; } = double.PositiveInfinity;
        public double MaxX { get; set; } = double.NegativeInfinity;
        public double Left { get; set; }
        public double Right { get; set; }
        public List<(double Y, string? Label)> Sections { get; } = new();
    }
}
