using Markdown.Renderer.Util;

namespace Markdown.Renderer.Test;

public class UnitTest1
{
    [Fact]
    public void ResourcesTest()
    {
        var cssContent = Resources.CssContent();
        Assert.NotEmpty(cssContent);
    }
}

public class MarkdownLinkMatcherTest
{
    [Theory]
    [InlineData("[Home](../../README.md)", true)]
    [InlineData("[directLink](Other/blafasel.md)", true)]
    [InlineData("[simple](simple.md)", true)]
    [InlineData("[simple](./simple.md)", true)]
    [InlineData("[simple](./hello/../simple.md)", true)]
    [InlineData("[trophy](../asdf/afewwr/../htarget.md)", true)]
    [InlineData("[sql](SQL%20Server/README.md)", true)]
    [InlineData("[framework](./ASP.NET/README.md)", true)]
    [InlineData("[release](./Charts/Release.v1.md)", true)]
    [InlineData("[external](https://example.com/README.md)", false)]
    [InlineData("[trophy](../asdf/af_ewwr/../target.md )", true)]
    [InlineData("[ trophy ]( ../asdf/afe-wwr/../target.md )", true)]
    
    public void BasicMatchTest(string input, bool expectedSuccess)
    {
        var matcher = new InternalLinkRegex();
        var result = matcher.Match(input);
        Assert.Equal(expectedSuccess, result.IsSuccess);
    }
}

public class LinkReplacerTest
{
    [Theory]
    [InlineData("[Guide](guide.md?view=raw#setup)", "[Guide](guide.html?view=raw#setup)")]
    [InlineData("[Home](README.md#start)", "[Home](index.html#start)")]
    [InlineData("[Database](SQL%20Server/README.md)", "[Database](SQL%20Server/index.html)")]
    [InlineData("[Framework](./ASP.NET/README.md)", "[Framework](./ASP.NET/index.html)")]
    [InlineData("[Release](./Charts/Release.v1.md)", "[Release](./Charts/Release.v1.html)")]
    [InlineData("[External](https://example.com/README.md)", "[External](https://example.com/README.md)")]
    public void ReplacesMarkdownExtensionAndPreservesSuffix(string input, string expected)
    {
        Assert.Equal(expected, new LinkReplacer().ReplaceAll(input));
    }
}
