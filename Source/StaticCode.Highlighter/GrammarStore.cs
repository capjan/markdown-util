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
            case "ts": case "typescript": return TypeScript();
            case "py": case "python": return Python();
            case "kt": case "kotlin": return Kotlin();
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

    private static Grammar TypeScript()
    {
        return new Grammar(
            keywords: new[]
            {
                "abstract", "any", "as", "async", "await", "boolean", "break", "case", "catch", "class",
                "const", "constructor", "continue", "debugger", "declare", "default", "delete", "do", "else",
                "enum", "export", "extends", "false", "finally", "for", "from", "function", "get", "if",
                "implements", "import", "in", "instanceof", "interface", "keyof", "let", "module", "namespace",
                "never", "new", "null", "number", "object", "private", "protected", "public", "readonly",
                "return", "set", "static", "string", "super", "switch", "symbol", "this", "throw", "true",
                "try", "type", "typeof", "undefined", "unique", "unknown", "var", "void", "while", "with",
                "yield"
            },
            operators: new[]
            {
                "===", "!==", ">>>", "**=", "...", "=>", "==", "!=", "<=", ">=", "++", "--", "&&", "||",
                "??", "?.", "**", "<<", ">>", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
                "=", "+", "-", "*", "/", "%", "<", ">", "!", "~", "&", "|", "^", "?", ":", ";", ",",
                ".", "(", ")", "[", "]", "{", "}"
            },
            strings: new[]
            {
                new LanguageString('"', "\\\"", "\\\\"),
                new LanguageString('\'', "\\\'", "\\\\"),
                new LanguageString('`', "\\`", "\\\\")
            },
            lineCommentStart: "//");
    }

    private static Grammar Python()
    {
        return new Grammar(
            keywords: new[]
            {
                "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
                "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import",
                "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while",
                "with", "yield"
            },
            operators: new[]
            {
                "**=", "//=", ">>=", "<<=", ":=", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=",
                "&=", "|=", "^=", "**", "//", "<<", ">>", "=", "+", "-", "*", "/", "%", "<", ">", "&",
                "|", "^", "~", ":", ";", ",", ".", "(", ")", "[", "]", "{", "}"
            },
            strings: new[]
            {
                new LanguageString('"', "\\\"", "\\\\"),
                new LanguageString('\'', "\\\'", "\\\\")
            },
            lineCommentStart: "#");
    }

    private static Grammar Kotlin()
    {
        return new Grammar(
            keywords: new[]
            {
                "as", "break", "by", "catch", "class", "companion", "const", "constructor", "continue", "data",
                "delegate", "do", "else", "enum", "external", "false", "field", "final", "finally", "for",
                "fun", "get", "if", "import", "in", "infix", "init", "inline", "inner", "interface", "internal",
                "is", "lateinit", "noinline", "null", "object", "open", "operator", "out", "override", "package",
                "private", "property", "protected", "public", "receiver", "reified", "return", "sealed", "set",
                "suspend", "tailrec", "this", "throw", "true", "try", "typealias", "val", "var", "vararg",
                "when", "where", "while"
            },
            operators: new[]
            {
                "===", "!==", "!in", "!is", "..<", "..", "!!", "?.", "?:", "::", "->", "==", "!=", "<=", ">=",
                "+=", "-=", "*=", "/=", "%=", "++", "--", "&&", "||", "=", "+", "-", "*", "/", "%", "<", ">",
                "!", "?", ":", ";", ",", ".", "(", ")", "[", "]", "{", "}"
            },
            strings: new[]
            {
                new LanguageString('"', "\\\"", "\\\\"),
                new LanguageString('\'', "\\\'", "\\\\")
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
