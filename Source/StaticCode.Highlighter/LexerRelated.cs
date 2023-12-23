namespace StaticCode.Highlighter;

public static class LexerRelated
{
    public static IEnumerable<LanguageToken> Analyse(this Lexer lexer, Stream stream)
    {
        using var streamReader = new StreamReader(stream) as TextReader;
        return lexer.Analyse(streamReader);
    }
    
    public static IEnumerable<LanguageToken> Analyse(this Lexer lexer, string code)
    {
        using var stringReader = new StringReader(code) as TextReader;
        return lexer.Analyse(stringReader).ToList();
    }
}