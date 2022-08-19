namespace StaticCode.Highlighter.Test;

public class LexerTests
{
    [Fact]
    public void CSharp()
    {
        var lexer = Lexer.ForLanguage("csharp");

        var code = Samples.CSharp();
        var token = lexer.Analyse(code).ToList();
        
        Assert.NotEmpty(token);
        Assert.Contains(token, itm => itm.Type == TokenType.Comment);
        Assert.Contains(token, itm => itm.Type == TokenType.String);
    }
}