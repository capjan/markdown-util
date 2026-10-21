namespace StaticCode.Highlighter;

public class LanguageString
{
    public LanguageString(char startEnd, params string[] escapeSequences)
    {
        StartEnd = startEnd;
        EscapeSequences = escapeSequences;
    }

    public char StartEnd { get; }
    public IReadOnlyCollection<string> EscapeSequences { get; }
}
public class Grammar
{
    public Grammar(
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<string> operators,
        IReadOnlyCollection<LanguageString> strings,
        string lineCommentStart,
        bool highlightKeys = false,
        bool highlightShellTokens = false,
        bool highlightMarkdown = false,
        bool ignoreKeywordCase = false,
        bool hyphenatedKeywords = false,
        string? blockCommentStart = null,
        string? blockCommentEnd = null,
        bool highlightHtml = false,
        bool highlightJavaTextBlocks = false,
        bool highlightRustCharLiterals = false)
    {
        Keywords = keywords;
        Operators = operators;
        Strings = strings;
        LineCommentStart = lineCommentStart;
        HighlightKeys = highlightKeys;
        HighlightShellTokens = highlightShellTokens;
        HighlightMarkdown = highlightMarkdown;
        IgnoreKeywordCase = ignoreKeywordCase;
        HyphenatedKeywords = hyphenatedKeywords;
        BlockCommentStart = blockCommentStart;
        BlockCommentEnd = blockCommentEnd;
        HighlightHtml = highlightHtml;
        HighlightJavaTextBlocks = highlightJavaTextBlocks;
        HighlightRustCharLiterals = highlightRustCharLiterals;
    }

    public IReadOnlyCollection<string> Keywords { get; }
    public IReadOnlyCollection<string> Operators { get; }
    
    public IReadOnlyCollection<LanguageString> Strings { get; }
    
    public string LineCommentStart { get; }

    public bool HighlightKeys { get; }

    public bool HighlightShellTokens { get; }

    public bool HighlightMarkdown { get; }

    public bool IgnoreKeywordCase { get; }

    public bool HyphenatedKeywords { get; }

    public string? BlockCommentStart { get; }

    public string? BlockCommentEnd { get; }

    public bool HighlightHtml { get; }

    public bool HighlightJavaTextBlocks { get; }

    public bool HighlightRustCharLiterals { get; }
}
