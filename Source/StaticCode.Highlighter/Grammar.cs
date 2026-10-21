namespace StaticCode.Highlighter;

public class LanguageString
{
    public LanguageString(char startEnd, params string[] escapeSequences)
        : this(startEnd.ToString(), false, escapeSequences)
    {
    }

    public LanguageString(char startEnd, bool isCharacterLiteral, params string[] escapeSequences)
        : this(startEnd.ToString(), isCharacterLiteral, escapeSequences)
    {
    }

    public LanguageString(string delimiter, params string[] escapeSequences)
        : this(delimiter, false, escapeSequences)
    {
    }

    private LanguageString(string delimiter, bool isCharacterLiteral, string[] escapeSequences)
    {
        Delimiter = delimiter;
        IsCharacterLiteral = isCharacterLiteral;
        EscapeSequences = escapeSequences;
    }

    public string Delimiter { get; }
    public char StartEnd => Delimiter[0];
    public bool IsCharacterLiteral { get; }
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
        IReadOnlyCollection<string>? declarationKeywords = null,
        IReadOnlyCollection<string>? atRules = null,
        string? blockCommentStart = null,
        string? blockCommentEnd = null,
        bool highlightHtml = false)
    {
        Keywords = keywords;
        Operators = operators;
        Strings = strings;
        LineCommentStart = lineCommentStart;
        HighlightKeys = highlightKeys;
        HighlightShellTokens = highlightShellTokens;
        HighlightMarkdown = highlightMarkdown;
        IgnoreKeywordCase = ignoreKeywordCase;
        DeclarationKeywords = declarationKeywords ?? Array.Empty<string>();
        AtRules = atRules ?? Array.Empty<string>();
        BlockCommentStart = blockCommentStart;
        BlockCommentEnd = blockCommentEnd;
        HighlightHtml = highlightHtml;
    }

    public IReadOnlyCollection<string> Keywords { get; }
    public IReadOnlyCollection<string> Operators { get; }
    
    public IReadOnlyCollection<LanguageString> Strings { get; }
    
    public string LineCommentStart { get; }

    public bool HighlightKeys { get; }

    public bool HighlightShellTokens { get; }

    public bool HighlightMarkdown { get; }

    public bool IgnoreKeywordCase { get; }

    public IReadOnlyCollection<string> DeclarationKeywords { get; }

    public IReadOnlyCollection<string> AtRules { get; }

    public string? BlockCommentStart { get; }

    public string? BlockCommentEnd { get; }

    public bool HighlightHtml { get; }
}
