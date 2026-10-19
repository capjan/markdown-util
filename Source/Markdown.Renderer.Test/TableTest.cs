namespace Markdown.Renderer.Test;

public class TableTest
{
    [Fact]
    public void WrapsPipeTablesInScrollableContainers()
    {
        const string markdown = "| Name | Value |\n| --- | --- |\n| Flowchart | `flowchart LR` |\n";

        var html = new HtmlRenderer().RenderMarkdownFragment(markdown).ReplaceLineEndings("\n");

        Assert.Contains("<div class=\"table-wrapper\">\n<table>", html);
        Assert.Contains("</table>\n</div>", html);
    }

    [Fact]
    public void WrapsGridTablesInScrollableContainers()
    {
        const string markdown = "+------+-------+\n| Name | Value |\n+======+=======+\n| One  | Two   |\n+------+-------+\n";

        var html = new HtmlRenderer().RenderMarkdownFragment(markdown).ReplaceLineEndings("\n");

        Assert.Contains("<div class=\"table-wrapper\">\n<table>", html);
        Assert.Contains("</table>\n</div>", html);
    }

    [Fact]
    public void PreservesBaseStylesForRawHtmlTables()
    {
        const string markdown = "<table>\n<tr><td>Raw table</td></tr>\n</table>\n";
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var renderer = new HtmlRenderer();

        try
        {
            var html = renderer.RenderMarkdownFragment(markdown);
            renderer.PrepareBasePath(outputPath);
            var css = File.ReadAllText(Path.Combine(outputPath, "css", "main.css")).ReplaceLineEndings("\n");

            Assert.Contains("<table>", html);
            Assert.DoesNotContain("table-wrapper", html);
            Assert.Contains("table {\n    width: 100%;\n    border-collapse: collapse;\n    margin: 1.5rem 0;\n}", css);
            Assert.Contains(".table-wrapper > table {\n    margin: 0;\n}", css);
        }
        finally
        {
            if (Directory.Exists(outputPath)) Directory.Delete(outputPath, recursive: true);
        }
    }
}
