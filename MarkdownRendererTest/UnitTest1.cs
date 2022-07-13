namespace MarkdownRendererTest;

public class UnitTest1
{
    [Fact]
    public void ResourcesTest()
    {
        using var cssStream = MarkdownRenderer.Resources.MainCssStream();
        Assert.NotNull(cssStream);
    }
}