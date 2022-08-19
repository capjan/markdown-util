using StaticCode.Highlighter.Res;

namespace StaticCode.Highlighter.Test;

public class ResourceLoaderTest
{
    [Fact]
    public void TestResources()
    {
        var cssContent = Resources.CssContent();
        Assert.NotEmpty(cssContent);
    }
}