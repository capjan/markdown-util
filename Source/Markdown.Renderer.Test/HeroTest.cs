using System.Text;

namespace Markdown.Renderer.Test;

public class HeroTest
{
    [Fact]
    public void RendersTitleTextAndActionsFromTheYamlOptions()
    {
        var html = Hero.Render("""
            title: Ship <docs>
            text: Plain Markdown & static HTML
            time: sunset
            height: 600px
            speed: 1.5
            waves: 2
            align: center
            actions:
              - text: Get started
                link: Guide/Setup.md#install
              - text: Overview
                link: ../README.md
              - text: GitHub
                link: https://github.com/capjan/mdu
                style: primary
            colors:
              water: "#0a3d62"
              sand: red
            """);

        Assert.StartsWith("<section class=\"hero hero-full hero-align-center\" style=\"--hero-height: 600px\"", html);
        Assert.Contains("data-hero-time=\"sunset\"", html);
        Assert.Contains("data-hero-speed=\"1.5\"", html);
        Assert.Contains("data-hero-waves=\"2\"", html);
        Assert.Contains("data-hero-water=\"#0a3d62\"", html);
        Assert.DoesNotContain("data-hero-sand", html);
        Assert.Contains("<p class=\"hero-title\">Ship &lt;docs&gt;</p>", html);
        Assert.Contains("<p class=\"hero-text\">Plain Markdown &amp; static HTML</p>", html);
        Assert.Contains("<a class=\"hero-button hero-button-primary\" href=\"Guide/Setup.html#install\">Get started</a>", html);
        Assert.Contains("<a class=\"hero-button\" href=\"../index.html\">Overview</a>", html);
        Assert.Contains("<a class=\"hero-button hero-button-primary\" href=\"https://github.com/capjan/mdu\" target=\"_blank\" rel=\"noopener noreferrer\">GitHub</a>", html);
        Assert.EndsWith("</section>", html);
    }

    [Theory]
    [InlineData("")]
    [InlineData("title: [unclosed")]
    [InlineData("time: noon\nquality: ultra\nheight: 99999\nspeed: fast\nwidth: wide")]
    public void FallsBackToDefaultsForMissingOrInvalidOptions(string options)
    {
        var html = Hero.Render(options);

        Assert.StartsWith("<section class=\"hero hero-full hero-align-left\" style=\"--hero-height: ", html);
        Assert.Contains("data-hero-time=\"auto\"", html);
        Assert.Contains("data-hero-quality=\"auto\"", html);
        Assert.Contains("data-hero-speed=\"1\"", html);
        Assert.Contains("<canvas class=\"hero-canvas\" aria-hidden=\"true\"></canvas>", html);
        Assert.DoesNotContain("hero-actions", html);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(1)")]
    [InlineData(" java\tscript:alert(1)")]
    [InlineData("\u0001javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    public void DropsActionsWhoseLinksRunScript(string link)
    {
        var html = Hero.Render($"actions:\n  - text: Click\n    link: \"{link.Replace("\t", "\\t").Replace("\u0001", "\\x01")}\"\n");

        Assert.DoesNotContain("hero-button", html);
        Assert.DoesNotContain("script:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Setup.md", "Setup.html")]
    [InlineData("#options", "#options")]
    [InlineData("https://example.com/a?b=c", "https://example.com/a?b=c")]
    [InlineData("mailto:docs@example.com", "mailto:docs@example.com")]
    public void KeepsRelativeWebAndMailLinks(string link, string href)
    {
        var html = Hero.Render($"actions:\n  - text: Click\n    link: \"{link}\"\n");

        Assert.Contains($"href=\"{href}\"", html);
    }

    [Fact]
    public void ClampsTheHeight()
    {
        Assert.Contains("--hero-height: 1200px", Hero.Render("height: 99999"));
        Assert.Contains("--hero-height: 240px", Hero.Render("height: 10"));
    }

    [Fact]
    public async Task PlacesAFullWidthHeroAboveTheContentAndAddsItsScript()
    {
        var page = await RenderPage("# Home\n\n```hero\ntitle: Welcome\n```\n\nIntro\n");

        var heroStart = page.IndexOf("<section class=\"hero hero-full", StringComparison.Ordinal);
        Assert.True(heroStart > 0);
        Assert.True(heroStart < page.IndexOf("<div class=\"site-layout", StringComparison.Ordinal));
        Assert.True(heroStart > page.IndexOf("<nav class=\"top-navigation", StringComparison.Ordinal));
        Assert.Contains("const fragmentSource", page);
        Assert.Contains("<h1 id=\"home\">", page);
        var styles = page.IndexOf("<style>.hero {", StringComparison.Ordinal);
        Assert.True(styles > 0 && styles < page.IndexOf("</head>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeepsAContentWidthHeroInThePage()
    {
        var page = await RenderPage("# Home\n\n```hero\ntitle: Welcome\nwidth: content\n```\n");

        var article = page.IndexOf("<article class=\"markdown-content\">", StringComparison.Ordinal);
        var hero = page.IndexOf("<section class=\"hero hero-inline", StringComparison.Ordinal);
        Assert.True(article > 0 && hero > article);
    }

    [Fact]
    public async Task LeavesPagesWithoutAHeroUnchanged()
    {
        var page = await RenderPage("# Home\n\n```yaml\ntitle: Welcome\n```\n");

        Assert.DoesNotContain("<section class=\"hero", page);
        Assert.DoesNotContain("const fragmentSource", page);
        Assert.DoesNotContain(".hero-canvas", page);
    }

    [Fact]
    public void KeepsHeroStylesOutOfTheSiteStylesheet()
    {
        Assert.DoesNotContain(".hero", Resources.CssContent());
        Assert.Contains(".hero-canvas", Hero.Styles);
    }

    private static async Task<string> RenderPage(string markdown)
    {
        var html = new StringBuilder();
        await using var writer = new StringWriter(html);
        await new HtmlRenderer().WriteHtml(writer, "Test", "", 0, markdown, "");
        return html.ToString();
    }
}
