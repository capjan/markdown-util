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

    [Theory]
    [InlineData("javascript", "const x = 1; return x;", "const|return", "1")]
    [InlineData("json", "{\"enabled\": true, \"count\": 1, \"missing\": null}", "true|null", "1")]
    [InlineData("sql", "Select name, 1 From users", "Select|From", "1")]
    [InlineData("html", "<div id='x'>text 1</div>", "div|id", "1")]
    [InlineData("css", "@media screen { .x { background-color: red; border-radius: 2px; } }", "media|background-color|border-radius", "2")]
    [InlineData("java", "var x = 1; return x;", "var|return", "1")]
    [InlineData("rust", "fn first<'a>(x: &'a dyn Trait) -> &'a str { let y = 1; x }", "fn|dyn|let", "1")]
    [InlineData("go", "func f() { x := 1; return x }", "func|return", "1")]
    public void HighlightsKeywordsAndNumbers(string language, string code, string expectedKeywords, string expectedNumber)
    {
        var tokens = Lexer.ForLanguage(language).Analyse(code).ToList();

        foreach (var keyword in expectedKeywords.Split('|'))
            Assert.True(tokens.Any(token => token.Text == keyword && token.Type == TokenType.Keyword), keyword);
        Assert.Contains(tokens, token => token.Text == expectedNumber && token.Type == TokenType.Number);
        Assert.Equal(code, string.Concat(tokens.Select(token => token.Text)));
    }

    [Fact]
    public void RustLifetimeIsNotHighlightedAsAString()
    {
        var tokens = Lexer.ForLanguage("rust").Analyse("fn first<'a>(x: &'a str)").ToList();

        Assert.DoesNotContain(tokens, token => token.Type == TokenType.String);
    }

    [Fact]
    public void HtmlHighlightsAttributeStringsWithoutTreatingTextApostrophesAsStrings()
    {
        var code = "<p title='x'>Don't panic</p>\n<p>It's fine title</p>";
        var tokens = Lexer.ForLanguage("html").Analyse(code).ToList();

        Assert.Contains(tokens, token => token.Text == "'x'" && token.Type == TokenType.String);
        Assert.DoesNotContain(tokens, token => token.Type == TokenType.String && token.Text.Contains("Don't", StringComparison.Ordinal));
        Assert.DoesNotContain(tokens, token => token.Type == TokenType.String && token.Text.Contains("It's", StringComparison.Ordinal));
        Assert.Contains(tokens, token => token.Text == "title" && token.Type == TokenType.Keyword);
        Assert.Contains(tokens, token => token.Text == "title" && token.Type == TokenType.Text);
        Assert.Equal(code, string.Concat(tokens.Select(token => token.Text)));
    }

    [Fact]
    public void HtmlAndCssBlockCommentsTakePrecedenceOverStrings()
    {
        var html = Lexer.ForLanguage("html").Analyse("<!-- 'not a string' -->").ToList();
        var css = Lexer.ForLanguage("css").Analyse("/* \"unclosed */ \"valid string\"").ToList();
        var htmlAttribute = Lexer.ForLanguage("html").Analyse("<p title=\"<!-- not a comment -->\">text</p>").ToList();
        var cssString = Lexer.ForLanguage("css").Analyse("content: \"/* not a comment */\";").ToList();

        Assert.All(html, token => Assert.Equal(TokenType.Comment, token.Type));
        Assert.DoesNotContain(html, token => token.Type == TokenType.String);
        Assert.Contains(css, token => token.Text == "/* \"unclosed */" && token.Type == TokenType.Comment);
        Assert.DoesNotContain(css, token => token.Type == TokenType.String && token.Text.Contains("unclosed", StringComparison.Ordinal));
        Assert.Equal("<!-- 'not a string' -->", string.Concat(html.Select(token => token.Text)));
        Assert.Equal("/* \"unclosed */ \"valid string\"", string.Concat(css.Select(token => token.Text)));
        Assert.Contains(css, token => token.Text == "\"valid string\"" && token.Type == TokenType.String);
        Assert.DoesNotContain(htmlAttribute, token => token.Type == TokenType.Comment);
        Assert.Equal("\"<!-- not a comment -->\"", string.Concat(htmlAttribute.Where(token => token.Type == TokenType.String).Select(token => token.Text)));
        Assert.DoesNotContain(cssString, token => token.Type == TokenType.Comment);
        Assert.Contains(cssString, token => token.Text == "\"/* not a comment */\"" && token.Type == TokenType.String);
    }

    [Fact]
    public void JavaHighlightsModernKeywordsAndTextBlocks()
    {
        var tokens = Lexer.ForLanguage("java").Analyse("record Item() { var text = \"\"\"\n return notAKeyword;\n\"\"\"; sealed class Example permits Item {} }").ToList();

        Assert.Contains(tokens, token => token.Text == "record" && token.Type == TokenType.Keyword);
        Assert.Contains(tokens, token => token.Text == "var" && token.Type == TokenType.Keyword);
        Assert.Contains(tokens, token => token.Text == "sealed" && token.Type == TokenType.Keyword);
        Assert.Contains(tokens, token => token.Text == "permits" && token.Type == TokenType.Keyword);
        Assert.DoesNotContain(tokens, token => token.Text == "return" && token.Type == TokenType.Keyword);
        Assert.Contains(tokens, token => token.Type == TokenType.String && token.Text.Contains("return notAKeyword;", StringComparison.Ordinal));
    }
}
