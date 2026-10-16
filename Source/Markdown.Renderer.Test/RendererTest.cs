using System.Text;

namespace Markdown.Renderer.Test;

public class RendererTest
{
    [Fact]
    public async Task BasicTest()
    {
        var markdown = Res.Resources.MarkdownWithCode;
        var renderer = new HtmlRenderer(); 
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await renderer.WriteHtml(sw, "test", "", 0, markdown, "");
        var renderedHtml = sb.ToString();
        
        Assert.Contains("<title>test</title>", renderedHtml);
        Assert.Contains("<h1", renderedHtml);
        Assert.Contains("Markdown with Syntax Highlighting", renderedHtml);
        Assert.Contains("<span class=\"string\">", renderedHtml);
        Assert.Contains("Hello World", renderedHtml);
    }

    [Fact]
    public async Task RendersTwoTrailingSpacesAsHardLineBreak()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "First line  \nSecond line", "");

        Assert.Contains("<p>First line<br />\nSecond line</p>", sb.ToString());
    }

}
