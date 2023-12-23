using System.Text.RegularExpressions;

namespace Markdown.Renderer.Util;

public class InternalLinkRegex
{
    private const string Pattern =
        @"(?<prefix>\[\s*(?<LinkName>[^\[]+?)\s*\]\(\s*)(?<Path>((\.{1,2}/)|([\w-]+/))+)?(?<FileName>(?<FilenameWithoutExtension>[\w-]+)(?<FileExtension>\.md))(?<postfix>\s*\))";

    public InternalLinkRegexMatch Match(string input)
    {
        var m = Regex.Match(input, Pattern);
        return MatchToConcreteMatch(m);
    }

    public string Replace(string input, Func<MarkdownLinkInfo, string> replacementFunction)
    {
        return Regex.Replace(input, Pattern, match =>
        {
            var info = MatchToMarkdownLinkInfo(match);
            return replacementFunction(info);
        });
    }

    private static MarkdownLinkInfo MatchToMarkdownLinkInfo(Match m)
    {
        return new MarkdownLinkInfo
        {
            FullMatch = m.Value,
            LinkName = m.Groups["LinkName"].Value,
            Path = m.Groups["Path"].Value,
            FileNameWithoutExtension = m.Groups["FilenameWithoutExtension"].Value,
            Extension = m.Groups["FileExtension"].Value,
            FileName = m.Groups["FileName"].Value
        };
    }
    
    private static InternalLinkRegexMatch MatchToConcreteMatch(Match m)
    {
        if (m.Success == false) return InternalLinkRegexMatch.NoRegexMatch;

        var info = new MarkdownLinkInfo
        {
            FullMatch = m.Value,
            LinkName = m.Groups["LinkName"].Value,
            Path = m.Groups["Path"].Value,
            FileNameWithoutExtension = m.Groups["FilenameWithoutExtension"].Value,
            Extension = m.Groups["FileExtension"].Value,
            FileName = m.Groups["FileName"].Value
        };
        return InternalLinkRegexMatch.Success(info);
    }
    
}