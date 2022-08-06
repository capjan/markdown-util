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