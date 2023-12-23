using Core.Extensions.TextRelated;

namespace StaticCode.Highlighter.Test;

public class HtmlFormatterTest
{
    [Fact]
    public void BasicTest()
    {
        var lexer = Lexer.ForLanguage("csharp");
        var code = Samples.CSharp();
        var tokens = lexer.Analyse(code);
        var formatter = new HtmlFormatter();
        var html = formatter.WriteToString(tokens);
        Assert.NotEmpty(html);
    }
}