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
        IReadOnlyCollection<LanguageString> strings, string lineCommentStart)
    {
        Keywords = keywords;
        Operators = operators;
        Strings = strings;
        LineCommentStart = lineCommentStart;
    }

    public IReadOnlyCollection<string> Keywords { get; }
    public IReadOnlyCollection<string> Operators { get; }
    
    public IReadOnlyCollection<LanguageString> Strings { get; }
    
    public string LineCommentStart { get; }
}