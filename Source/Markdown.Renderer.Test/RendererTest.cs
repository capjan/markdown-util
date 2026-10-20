using System.Text;
using Markdown.Renderer.Res.Model;

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

    [Fact]
    public async Task OmitsFootnoteReferencesFromTableOfContentsTitles()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0,
            "# Dashboard\n\n## Produkte[^1]\n\nText\n\n[^1]: Note", "");

        var toc = sb.ToString().Split("class=\"toc-list\"", 2)[1].Split("</ul>", 2)[0];
        Assert.Contains("class=\"toc-link\" href=\"#produkte\">Produkte</a>", toc);
        Assert.DoesNotContain("<sup>", toc);
    }

    [Fact]
    public async Task OmitsBreadcrumbsThatOnlyNameTheCurrentPage()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Dashboard", "",
            breadcrumbs: [new BreadcrumbItem("Dashboard", null)]);

        Assert.DoesNotContain("class=\"breadcrumbs\"", sb.ToString());
    }

    [Fact]
    public async Task RendersBreadcrumbsWithAncestors()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 1, "# Guide", "",
            breadcrumbs: [new BreadcrumbItem("Dashboard", "../index.html"), new BreadcrumbItem("Guide", null)]);

        Assert.Contains("<nav class=\"breadcrumbs\" aria-label=\"Breadcrumb\"><a href=\"../index.html\">Dashboard</a>", sb.ToString());
    }

}
