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
    [InlineData("[trophy](../asdf/af_ewwr/../target.md )", true)]
    [InlineData("[ trophy ]( ../asdf/afe-wwr/../target.md )", true)]
    
    public void BasicMatchTest(string input, bool expectedSuccess)
    {
        var matcher = new InternalLinkRegex();
        var result = matcher.Match(input);
        Assert.Equal(expectedSuccess, result.IsSuccess);
    }
}