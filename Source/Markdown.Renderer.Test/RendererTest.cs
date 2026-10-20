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
    public async Task AppliesConfiguredBackgroundToHeaderSurfacesForLightAndDarkThemes()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var renderer = new HtmlRenderer();
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);

        try
        {
            renderer.PrepareBasePath(outputPath, new SiteConfiguration
            {
                Light = new SitePalette { Background = "#123456" },
                Dark = new SitePalette { Background = "#654321" }
            });
            await renderer.WriteHtml(sw, "test", "", 0, "# Test", "");

            var html = sb.ToString();
            Assert.Contains(":root,:root[data-theme=\"light\"]", html);
            Assert.Contains("--surface-header:#123456", html);
            Assert.Contains(":root[data-theme=\"dark\"]", html);
            Assert.Contains("--surface-header:#654321", html);
            Assert.Contains("@media(prefers-color-scheme:dark)", html);
        }
        finally
        {
            if (Directory.Exists(outputPath)) Directory.Delete(outputPath, recursive: true);
        }
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
    public async Task AddsPermalinksToSectionHeadingsOnly()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Dashboard\n\n## Produkte", "");

        var html = sb.ToString();
        Assert.Matches("<h1[^>]*>Dashboard</h1>", html);
        Assert.Contains("Produkte<a class=\"headerlink\" href=\"#produkte\" title=\"Permanent link\" aria-label=\"Permanent link\"><svg viewBox=\"0 0 20 20\" aria-hidden=\"true\"", html);
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

    [Fact]
    public async Task RendersFooterWithNeighborPagesDateAndEditLink()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Guide", "https://example.com/edit/Guide.md",
            navigationItems:
            [
                new NavigationItem { Title = "Home", Url = "index.html" },
                new NavigationItem { Title = "Guide", Url = "Guide.html", IsCurrent = true },
                new NavigationItem { Title = "Tips & Tricks", Url = "Tips.html" }
            ],
            lastModified: new DateTimeOffset(2026, 10, 3, 14, 0, 0, TimeSpan.FromHours(2)));

        var footer = sb.ToString().Split("<footer class=\"page-footer\">", 2)[1].Split("</footer>", 2)[0];
        Assert.Contains("Last updated on <time datetime=\"2026-10-03\">October 3, 2026</time>", footer);
        Assert.Contains("<a href=\"https://example.com/edit/Guide.md\" target=\"_blank\" rel=\"noopener noreferrer\">Edit this page</a>", footer);
        Assert.Contains("<a class=\"page-pagination-link is-previous\" href=\"index.html\" rel=\"prev\"><span class=\"page-pagination-label\">Previous</span><span class=\"page-pagination-title\">Home</span></a>", footer);
        Assert.Contains("<a class=\"page-pagination-link is-next\" href=\"Tips.html\" rel=\"next\"><span class=\"page-pagination-label\">Next</span><span class=\"page-pagination-title\">Tips &amp; Tricks</span></a>", footer);
    }

    [Fact]
    public async Task OmitsFooterWithoutNeighborPagesDateOrEditLink()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Only page", "",
            navigationItems: [new NavigationItem { Title = "Only page", Url = "index.html", IsCurrent = true }]);

        Assert.DoesNotContain("page-footer", sb.ToString());
    }

    [Fact]
    public async Task AddsUnlistedTitleToFootnotes()
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        await new HtmlRenderer().WriteHtml(sw, "test", "", 0, "# Page\n\nText[^1]\n\n[^1]: Note", "");

        var html = sb.ToString();
        Assert.Contains("<div class=\"footnotes\">\n<hr />\n<h2 class=\"footnotes-title\">Footnotes</h2>", html);
        Assert.DoesNotContain(">Footnotes</a>", html.Split("class=\"toc-list\"", 2).ElementAtOrDefault(1) ?? string.Empty);
    }

}
