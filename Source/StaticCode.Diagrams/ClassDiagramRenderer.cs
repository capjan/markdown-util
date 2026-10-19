using System.Text.RegularExpressions;
using StaticCode.Diagrams.Flowchart;

namespace StaticCode.Diagrams;

internal static class ClassDiagramRenderer
{
    private static readonly Regex Relationship = new(
        @"^(?<left>[\p{L}\p{N}_.]+)\s*(?<relation><\|--|--\|>|\*--|--\*|o--|--o|<\.\.|\.\.>|<--|-->|--|\.\.|<\|\.\.|\.\.\|>)\s*(?<right>[\p{L}\p{N}_.]+)(?:\s*:\s*(?<label>.*))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ClassDeclaration = new(@"^class\s+(?<id>[\p{L}\p{N}_.]+)\s*$", RegexOptions.Compiled);

    public static string Render(string source)
    {
        var nodes = new Dictionary<string, FlowchartNode>(StringComparer.Ordinal);
        var edges = new List<FlowchartEdge>();
        foreach (var rawLine in source.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal) || line == "classDiagram" || line == "classDiagram-v2") continue;
            var declaration = ClassDeclaration.Match(line);
            if (declaration.Success)
            {
                GetNode(declaration.Groups["id"].Value);
                continue;
            }

            var relationship = Relationship.Match(line);
            if (!relationship.Success) throw new DiagramSyntaxException("Unsupported class diagram statement.", 1);
            var left = GetNode(relationship.Groups["left"].Value);
            var right = GetNode(relationship.Groups["right"].Value);
            var kind = relationship.Groups["relation"].Value;
            var isGeneralization = kind is "<|--" or "--|>" or "<|.." or "..|>";
            var swapEndpoints = kind is "<.." or "<--" or "--|>" or "..|>";
            var from = swapEndpoints ? right.Id : left.Id;
            var to = swapEndpoints ? left.Id : right.Id;
            var lineStyle = kind.Contains('.') ? LineStyle.Dotted : LineStyle.Solid;
            var leftMarker = kind.StartsWith("*", StringComparison.Ordinal) ? EdgeMarker.Diamond
                : kind.StartsWith("o", StringComparison.Ordinal) ? EdgeMarker.HollowDiamond
                : kind.StartsWith('<') ? EdgeMarker.Arrow
                : EdgeMarker.None;
            var rightMarker = kind.EndsWith("*", StringComparison.Ordinal) ? EdgeMarker.Diamond
                : kind.EndsWith("o", StringComparison.Ordinal) ? EdgeMarker.HollowDiamond
                : kind.EndsWith(">", StringComparison.Ordinal) ? EdgeMarker.Arrow
                : EdgeMarker.None;
            var label = relationship.Groups["label"].Success ? relationship.Groups["label"].Value.Trim() : null;
            var startMarker = isGeneralization ? EdgeMarker.Triangle : swapEndpoints ? rightMarker : leftMarker;
            var endMarker = isGeneralization ? EdgeMarker.None : swapEndpoints ? leftMarker : rightMarker;
            edges.Add(new FlowchartEdge(from, to, label, lineStyle, startMarker, endMarker, 1));
        }

        if (nodes.Count == 0) throw new DiagramSyntaxException("The class diagram has no classes.", 1);
        return FlowchartSvgWriter.Write(new FlowchartDiagram(FlowDirection.TopToBottom, nodes.Values.ToArray(), edges, "Class diagram"));

        FlowchartNode GetNode(string id)
        {
            if (nodes.TryGetValue(id, out var node)) return node;
            node = new FlowchartNode(id);
            nodes.Add(id, node);
            return node;
        }
    }
}
