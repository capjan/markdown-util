using Markdig;
using Markdown.Renderer.MarkdigExtension;

namespace Markdown.Renderer.Test;

public class AlertTest
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseGitHubAlerts()
        .Build();

    private static string Render(string markdown) => Markdig.Markdown.ToHtml(markdown, Pipeline).ReplaceLineEndings("\n");

    [Theory]
    [InlineData("NOTE", "note", "Note")]
    [InlineData("TIP", "tip", "Tip")]
    [InlineData("IMPORTANT", "important", "Important")]
    [InlineData("WARNING", "warning", "Warning")]
    [InlineData("CAUTION", "caution", "Caution")]
    public void RendersAllGitHubAlertTypes(string marker, string name, string title)
    {
        var html = Render($"> [!{marker}]\n> Useful information.\n");

        Assert.Equal(
            $"<div class=\"markdown-alert markdown-alert-{name}\">\n" +
            $"<p class=\"markdown-alert-title\">{title}</p>\n" +
            "<p>Useful information.</p>\n" +
            "</div>\n",
            html);
    }

    [Theory]
    [InlineData("> [!note]\n> Text")]
    [InlineData("> [!Note]  \n> Text")]
    [InlineData("> [!NOTE]\t\n> Text")]
    [InlineData("> [!NOTE]\nLazy continuation text")]
    [InlineData("> [!NOTE]\n>\n> Text")]
    public void AcceptsGitHubMarkerVariants(string markdown)
    {
        var html = Render(markdown);

        Assert.Contains("<div class=\"markdown-alert markdown-alert-note\"", html);
        Assert.DoesNotContain("[!", html);
        Assert.DoesNotContain("<blockquote>", html);
        Assert.DoesNotContain("<p></p>", html);
        Assert.DoesNotContain("<svg", html);
    }

    [Theory]
    [InlineData("> [!NOTE] Text on the marker line")]
    [InlineData("> [!NOTE]")]
    [InlineData("> [!INFO]\n> Unknown type")]
    [InlineData("> [! NOTE]\n> Text")]
    [InlineData("> \\[!NOTE]\n> Escaped marker")]
    [InlineData("> Text\n> [!NOTE]\n> Marker not on the first line")]
    [InlineData("- > [!NOTE]\n  > Nested in a list")]
    [InlineData("> > [!NOTE]\n> > Nested in a block quote")]
    [InlineData("[!NOTE]\nNo block quote")]
    public void KeepsNonAlertsAsRegularMarkdown(string markdown)
    {
        var html = Render(markdown);

        Assert.DoesNotContain("markdown-alert", html);
    }

    [Theory]
    [InlineData("> [!INFO]\n> Unknown type", "<blockquote>\n<p>[!INFO]\nUnknown type</p>\n</blockquote>\n")]
    [InlineData("> [!note]", "<blockquote>\n<p>[!note]</p>\n</blockquote>\n")]
    [InlineData("> [!Custom]\n>\n> Text", "<blockquote>\n<p>[!Custom]</p>\n<p>Text</p>\n</blockquote>\n")]
    public void PreservesMarkerTextOfNonAlerts(string markdown, string expected)
    {
        Assert.Equal(expected, Render(markdown));
    }

    [Fact]
    public void RendersBlockContentInsideAlert()
    {
        var html = Render("> [!WARNING]\n> First paragraph with **bold** and `code`.\n>\n> - Item one\n> - Item two\n");

        Assert.Contains("<p class=\"markdown-alert-title\">Warning</p>\n<p>First paragraph with <strong>bold</strong> and <code>code</code>.</p>\n<ul>", html);
        Assert.EndsWith("</ul>\n</div>\n", html);
    }

    [Fact]
    public void RendersMultipleAlertsAndPlainBlockQuotes()
    {
        var html = Render("> [!TIP]\n> Tip text\n\n> Quote text\n\n> [!CAUTION]\n> Caution text\n");

        Assert.Contains("markdown-alert-tip", html);
        Assert.Contains("<blockquote>\n<p>Quote text</p>\n</blockquote>", html);
        Assert.Contains("markdown-alert-caution", html);
    }

    [Fact]
    public async Task HtmlRendererSupportsAlerts()
    {
        var sb = new System.Text.StringBuilder();
        await using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Title\n\n> [!IMPORTANT]\n> Read this first.\n", "");

        Assert.Contains("<p class=\"markdown-alert-title\">Important</p>", sb.ToString());
    }
}
