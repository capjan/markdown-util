using System.Text;

namespace Markdown.Renderer.Test;

public class PdfRendererTest
{
    [Fact]
    public void RendersTablesInsideScrollableContainersToPdf()
    {
        using var output = new MemoryStream();

        new PdfRenderer(new HtmlRenderer()).WritePdf(output, "Tables",
        [
            new PdfSourceSection("Tables", "tables/index.html", "| Header | Value |\n| --- | --- |\n| Wrapped table | preserved |\n")
        ], includeFrontMatter: false);

        var pdf = Encoding.Latin1.GetString(output.ToArray());
        Assert.Contains("<57726170706564207461626C65> Tj", pdf);
        Assert.Contains("<707265736572766564> Tj", pdf);
    }

    [Fact]
    public void WritesMduContentToPdf17WithLinksAndVectors()
    {
        const string markdown = """
            # Guide

            A **bold** paragraph with [an external link](https://example.com) and a [local link](../Other.md#target).

            - [x] Completed
            - [ ] Pending

            ![Photo](https://example.com/photo.png)

            ```csharp
            var title = "Hello";
            ```

            > [!TIP]
            > Keep this note.

            ```mermaid
            flowchart TD
            A[Start] --> B[Finish]
            ```

            ```mermaid
            sequenceDiagram
            participant User
            participant Api
            User->>Api: Request
            alt Cached
            Api-->>User: Cached response
            else Fresh
            Api->>Api: Load data
            Api-->>User: Fresh response
            end
            ```

            $$
            x^2 + 1
            $$
            """;
        using var output = new MemoryStream();

        new PdfRenderer(new HtmlRenderer()).WritePdf(output, "Guide",
        [
            new PdfSourceSection("Guide", "guide/index.html", markdown),
            new PdfSourceSection("Other", "Other.html", "# Target\n\nDestination page.")
        ]);

        var pdf = Encoding.Latin1.GetString(output.ToArray());
        Assert.StartsWith("%PDF-1.7", pdf);
        Assert.Contains("<496E68616C74737665727A656963686E6973>", pdf);
        Assert.Contains("<4775696465>", pdf);
        Assert.Contains("/Subtype /Link", pdf);
        Assert.Contains(" /Annots [", pdf);
        Assert.Contains("/Dest [", pdf);
        Assert.Contains("<5B785D> Tj", pdf);
        Assert.Contains("<5B> Tj", pdf);
        Assert.Contains("<5D> Tj", pdf);
        Assert.Contains("https://example.com/photo.png", pdf);
        Assert.Contains(" c\n", pdf);
        Assert.Contains("xref\n", pdf);
        Assert.EndsWith("%%EOF\n", pdf);
    }
}
