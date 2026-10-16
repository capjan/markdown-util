namespace StaticCode.Highlighter;

public static class GrammarStore
{
    /// <summary>
    /// Returns the Language Settings for the given language name followed in markdown after the tree backticks
    /// </summary>
    /// <param name="name">name of the language <see href="https://rdmd.readme.io/docs/code-blocks"/></param>
    /// <returns>The resolved language setting or plain if not found</returns>
    public static Grammar GetByName(string name)
    {
        switch (name)
        {
            case "cs": case "csharp": return CSharp();
            case "swift": return Swift();
            case "yaml": case "yml": return Yaml();
            case "bash": case "zsh": case "sh": case "shell": return Shell();
            case "md": case "markdown": return Markdown();
            default: return Plain();
        }
    }

    private static Grammar Markdown()
    {
        return new Grammar(
            keywords: Array.Empty<string>(),
            operators: Array.Empty<string>(),
            strings: Array.Empty<LanguageString>(),
            lineCommentStart: string.Empty,
            highlightMarkdown: true);
    }
    
    private static Grammar CSharp()
    {
        return new Grammar(
            keywords: new[]
            {
                "if", "var", "int", "string", "return"
            },
            operators: new[]
            {
                "-", "+", "/", "*", "==", "=", ";"
            },
            strings: new []{
                new LanguageString(
                    startEnd: '"',
                    escapeSequences: "\\\"")
            },
            lineCommentStart: "//");
    }
    
    private static Grammar Swift()
    {
        return new Grammar(
            keywords: new[]
            {
                "if", "let", "var", "do", "try", "catch", "Int", "String", "return"
            },
            operators: new[]
            {
                "-", "+", "/", "*", "==", "=", ";"
            },
            strings: new []{
                new LanguageString(
                    startEnd: '"',
                    escapeSequences: "\\\"")
            },
            lineCommentStart: "//");
    }

    private static Grammar Yaml()
    {
        return new Grammar(
            keywords: new[]
            {
                "true", "false", "null"
            },
            operators: new[]
            {
                "-", "[", "]", "{", "}", ",", "?", "|", ">", "&", "*", "!"
            },
            strings: new[]
            {
                new LanguageString('"', "\\\"", "\\\\", "\\n", "\\t", "\\r"),
                new LanguageString('\'', "''")
            },
            lineCommentStart: "#",
            highlightKeys: true);
    }

    private static Grammar Shell()
    {
        return new Grammar(
            keywords: new[]
            {
                "if", "then", "else", "elif", "fi", "for", "while", "until", "do", "done",
                "case", "esac", "in", "function", "select", "time", "coproc"
            },
            operators: new[]
            {
                ";;&", ";;", ";&", "<<<", "&&", "||", ">>", "<<", "&>", ">&", "<&", ">|",
                "|", "&", ";", ">", "<", "(", ")", "{", "}", "!", "="
            },
            strings: new[]
            {
                new LanguageString('"', "\\\"", "\\\\"),
                new LanguageString('\'')
            },
            lineCommentStart: "#",
            highlightShellTokens: true);
    }

    private static Grammar Plain()
    {
        return new Grammar(
            keywords: Array.Empty<string>(),
            operators: Array.Empty<string>(),
            strings: Array.Empty<LanguageString>(),
            lineCommentStart: string.Empty
        );
    }


}
