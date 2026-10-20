namespace Markdown.Renderer.Test;

public class LinkTest
{
    [Theory]
    [InlineData("guide.md?view=raw#setup", "guide.html?view=raw#setup")]
    [InlineData("README.md#start", "index.html#start")]
    [InlineData("../../README.md", "../../index.html")]
    [InlineData("Other/blafasel.md", "Other/blafasel.html")]
    [InlineData("./hello/../simple.md", "./hello/../simple.html")]
    [InlineData("SQL%20Server/README.md", "SQL%20Server/index.html")]
    [InlineData("./ASP.NET/README.md", "./ASP.NET/index.html")]
    [InlineData("./Charts/Release.v1.md", "./Charts/Release.v1.html")]
    [InlineData("https://example.com/README.md", "https://example.com/README.md")]
    [InlineData("/Docs/Setup.md", "/Docs/Setup.md")]
    [InlineData("diagram.svg", "diagram.svg")]
    [InlineData("#setup", "#setup")]
    public void ReplacesMarkdownExtensionAndPreservesSuffix(string url, string expected)
    {
        Assert.Equal(expected, LinkReplacer.ReplaceUrl(url));
    }

    [Theory]
    [InlineData("[Setup](../Setup.md)", "href=\"../Setup.html\"")]
    [InlineData("[ Setup ]( ../Setup.md )", "href=\"../Setup.html\"")]
    [InlineData("[Home](../README.md)", "href=\"../index.html\"")]
    [InlineData("[Install](Setup.md#install)", "href=\"Setup.html#install\"")]
    [InlineData("[Setup][setup]\n\n[setup]: ../Setup.md", "href=\"../Setup.html\"")]
    [InlineData("- [Setup](../Setup.md)", "href=\"../Setup.html\"")]
    [InlineData("| Page |\n| --- |\n| [Setup](../Setup.md) |", "href=\"../Setup.html\"")]
    [InlineData("> [!NOTE]\n> [Setup](../Setup.md)", "href=\"../Setup.html\"")]
    [InlineData("Text[^1]\n\n[^1]: [Setup](../Setup.md)", "href=\"../Setup.html\"")]
    public void RewritesLinksToMarkdownFiles(string markdown, string expected)
    {
        var html = new HtmlRenderer().RenderMarkdownFragment(markdown);

        Assert.Contains(expected, html);
        Assert.DoesNotContain("Setup.md", html);
        Assert.DoesNotContain("README.md", html);
    }

    [Theory]
    [InlineData("```\n[Setup](../Setup.md)\n```")]
    [InlineData("```markdown\n[Setup](../Setup.md)\n```")]
    [InlineData("    [Setup](../Setup.md)")]
    [InlineData("Write `[Setup](../Setup.md)` to link a page.")]
    public void KeepsMarkdownLinksInsideCodeUnchanged(string markdown)
    {
        var html = new HtmlRenderer().RenderMarkdownFragment(markdown);

        Assert.Contains("Setup.md", html);
        Assert.DoesNotContain("Setup.html", html);
    }

    [Fact]
    public void RewritesLinkNextToCodeSpanShowingTheSameLink()
    {
        var html = new HtmlRenderer().RenderMarkdownFragment(
            "Write `[Setup](../Setup.md)` to get [Setup](../Setup.md).");

        Assert.Contains("<code>[Setup](../Setup.md)</code>", html);
        Assert.Contains("href=\"../Setup.html\"", html);
    }
}
