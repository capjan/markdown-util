using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdown.Linter;
using StaticCode.Diagrams;

namespace MarkdownUtil.Service;

public sealed record CompatibilityIssue(int LineNumber, string Code, string Message);

public static class MarkdownCompatibility
{
    private static readonly Regex AdmonitionHeader = new(
        @"^(?<indent> *)(?<marker>!!!|\?\?\?\+?)[ \t]*(?<type>[^ \t]+)?(?:[ \t]+(?<title>.*))?$",
        RegexOptions.Compiled);
    private static readonly Regex TabHeader = new(
        "^(?<indent> *)===[ \\t]+[\\\"'](?<title>.*?)[\\\"'][ \\t]*$",
        RegexOptions.Compiled);
    private static readonly Regex CardGrid = new(
        """<div\s+class=["']grid\s+cards["']\s+markdown\s*>(?<body>[\s\S]*?)</div>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Fence = new(@"^\s*(```+|~~~+)", RegexOptions.Compiled);
    private static readonly Regex BlockquotePrefix = new(@"^\s*(?:>\s*)*", RegexOptions.Compiled);
    private static readonly Regex AnnotationMarker = new(@"^\s*(?:>\s*)*\{ \.annotate \}\s*$", RegexOptions.Compiled);

    public static IReadOnlyList<CompatibilityIssue> FindIssues(string content)
    {
        var issues = new List<CompatibilityIssue>();
        var lines = content.ReplaceLineEndings("\n").Split('\n');
        var inFence = false;
        string fence = string.Empty;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var fenceMatch = Fence.Match(BlockquotePrefix.Replace(line, string.Empty));
            if (fenceMatch.Success)
            {
                if (!inFence)
                {
                    inFence = true;
                    fence = fenceMatch.Groups[1].Value[..3];
                }
                else if (fenceMatch.Groups[1].Value.StartsWith(fence, StringComparison.Ordinal))
                {
                    inFence = false;
                }

                continue;
            }

            if (inFence) continue;
            if (line.TrimStart().StartsWith("!!!", StringComparison.Ordinal) ||
                line.TrimStart().StartsWith("???", StringComparison.Ordinal))
                issues.Add(new CompatibilityIssue(index + 1, RuleCodes.MaterialAdmonition, "Material for MkDocs admonition syntax is unsupported; run 'mdu fix' to convert it to a GitHub Alert."));
            else if (line.TrimStart().StartsWith("=== ", StringComparison.Ordinal))
                issues.Add(new CompatibilityIssue(index + 1, RuleCodes.MaterialTabs, "Material for MkDocs tab syntax is unsupported; run 'mdu fix' to convert tabs to headings."));
            else if (AnnotationMarker.IsMatch(line))
                issues.Add(new CompatibilityIssue(index + 1, RuleCodes.CodeAnnotations, "Material code annotations are unsupported."));
        }

        foreach (Match match in CardGrid.Matches(content))
        {
            var line = 1 + content[..match.Index].Count(character => character == '\n');
            issues.Add(new CompatibilityIssue(line, RuleCodes.MaterialCards, "Material card grids are unsupported; run 'mdu fix' to convert them to Markdown lists."));
        }

        var document = Markdig.Markdown.Parse(content, new MarkdownPipelineBuilder().Build());
        foreach (var block in document.Descendants<FencedCodeBlock>())
        {
            if (!string.Equals(block.Info?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase)) continue;
            var source = GetCode(block);
            if (!DiagramRenderer.TryRenderSvg("mermaid", source, out _))
                issues.Add(new CompatibilityIssue(block.Line + 1, RuleCodes.UnsupportedMermaid, "This Mermaid diagram uses syntax that mdu cannot render yet."));
        }

        return issues;
    }

    public static string FixMarkdown(string content)
    {
        content = CardGrid.Replace(content, match => ConvertCardGrid(match.Groups["body"].Value));
        content = ConvertBlockSyntax(content, convertTabs: true);
        content = ConvertBlockSyntax(content, convertTabs: false);
        content = RemoveMaterialAnnotationMarkers(content);
        return content;
    }

    private static string RemoveMaterialAnnotationMarkers(string content)
    {
        var output = new List<string>();
        var inFence = false;
        string fence = string.Empty;
        foreach (var line in content.ReplaceLineEndings("\n").Split('\n'))
        {
            var fenceMatch = Fence.Match(BlockquotePrefix.Replace(line, string.Empty));
            if (fenceMatch.Success)
            {
                if (!inFence)
                {
                    inFence = true;
                    fence = fenceMatch.Groups[1].Value[..3];
                }
                else if (fenceMatch.Groups[1].Value.StartsWith(fence, StringComparison.Ordinal))
                {
                    inFence = false;
                }
                output.Add(line);
                continue;
            }

            if (!inFence && AnnotationMarker.IsMatch(line)) continue;
            output.Add(line);
        }

        return string.Join('\n', output);
    }

    private static string ConvertCardGrid(string body)
    {
        var result = new List<string>();
        List<string>? current = null;
        foreach (var rawLine in body.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (current is not null) AddItem(current);
                current = [line[2..].TrimEnd()];
            }
            else if (current is not null && line.Length > 0)
            {
                current.Add(line);
            }
        }

        if (current is not null) AddItem(current);
        return string.Join(Environment.NewLine, result);

        void AddItem(List<string> item)
        {
            var text = string.Join(" ", item).Trim();
            result.Add($"- {text}");
        }
    }

    private static string ConvertBlockSyntax(string content, bool convertTabs) =>
        ProcessLines(content.ReplaceLineEndings("\n").Split('\n'), convertTabs, 0);

    private static string ProcessLines(string[] lines, bool convertTabs, int startingIndent)
    {
        var output = new List<string>();
        var inFence = false;
        string fence = string.Empty;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var fenceMatch = Fence.Match(BlockquotePrefix.Replace(line, string.Empty));
            if (fenceMatch.Success)
            {
                if (!inFence)
                {
                    inFence = true;
                    fence = fenceMatch.Groups[1].Value[..3];
                }
                else if (fenceMatch.Groups[1].Value.StartsWith(fence, StringComparison.Ordinal))
                {
                    inFence = false;
                }
            }

            var match = !inFence
                ? (convertTabs ? TabHeader.Match(line) : AdmonitionHeader.Match(line))
                : Match.Empty;
            if (!match.Success)
            {
                output.Add(line);
                continue;
            }

            var indent = match.Groups["indent"].Value.Length;
            if (indent < startingIndent)
            {
                output.Add(line);
                continue;
            }

            var body = new List<string>();
            var bodyEnd = index + 1;
            var sawBody = false;
            while (bodyEnd < lines.Length)
            {
                var candidate = lines[bodyEnd];
                var candidateIndent = candidate.Length - candidate.TrimStart(' ').Length;
                if (candidate.Trim().Length > 0 && candidateIndent <= indent) break;
                if (candidate.Trim().Length > 0) sawBody = true;
                body.Add(candidate);
                bodyEnd++;
            }

            var dedent = indent + (sawBody ? 4 : 0);
            var bodyLines = body.Select(candidate => candidate.Trim().Length == 0
                ? string.Empty
                : candidate[Math.Min(dedent, candidate.Length)..]).ToArray();
            var convertedBody = ProcessLines(bodyLines, convertTabs, 0).TrimEnd('\n');
            var title = match.Groups["title"].Success ? match.Groups["title"].Value.Trim() : string.Empty;
            if (title.Length >= 2 && title[0] == title[^1] && title[0] is '\'' or '"')
                title = title[1..^1];

            if (convertTabs)
            {
                output.Add(new string(' ', indent) + "### " + title);
                if (convertedBody.Length > 0) output.AddRange(convertedBody.Split('\n'));
            }
            else
            {
                var kind = match.Groups["type"].Value.Trim().TrimEnd(':').ToLowerInvariant();
                var alert = kind switch
                {
                    "tip" => "TIP",
                    "warning" => "WARNING",
                    "danger" or "bug" => "CAUTION",
                    _ => "NOTE"
                };
                output.Add(new string(' ', indent) + $"> [!{alert}]");
                if (title.Length > 0) output.Add(new string(' ', indent) + $"> **{title}**");
                if (convertedBody.Length > 0)
                    output.AddRange(convertedBody.Split('\n').Select(bodyLine => new string(' ', indent) + (bodyLine.Length == 0 ? ">" : $"> {bodyLine}")));
            }

            if (bodyEnd < lines.Length && lines[bodyEnd].Trim().Length > 0)
                output.Add(string.Empty);

            index = bodyEnd - 1;
        }

        return string.Join('\n', output);
    }

    private static string GetCode(FencedCodeBlock block)
    {
        if (block.Lines.Lines is null) return string.Empty;
        return string.Join('\n', block.Lines.Lines.Select(line => line.Slice.ToString()));
    }
}
