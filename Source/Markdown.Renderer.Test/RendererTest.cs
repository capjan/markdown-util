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
        
        Assert.NotEmpty(renderedHtml);
    }
}