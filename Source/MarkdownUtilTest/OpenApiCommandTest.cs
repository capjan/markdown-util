using Markdown.Renderer;
using Markdown.Search;
using MarkdownUtil.Commands;

namespace MarkdownUtilTest;

public class OpenApiCommandTest
{
    [Fact]
    public void RendersOpenApiSpecificationToStaticHtmlSite()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mdu-openapi-command-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(directory, "site");
        Directory.CreateDirectory(directory);
        try
        {
            var specificationPath = Path.Combine(directory, "orders.yaml");
            File.WriteAllText(specificationPath, """
                openapi: 3.1.0
                info:
                  title: Orders API
                  version: 1.0
                paths:
                  /orders:
                    get:
                      tags: [Orders]
                      summary: List orders
                      responses:
                        '200':
                          description: OK
                """);

            var command = new OpenApiCommand(new HtmlRenderer(), new SearchIndexBuilder());
            var result = command.Execute(specificationPath, outputDirectory);

            Assert.Equal(0, result);
            var html = File.ReadAllText(Path.Combine(outputDirectory, "index.html"));
            Assert.Contains("<title>orders</title>", html);
            Assert.Contains("<strong>Orders API</strong>", html);
            Assert.Contains("<code>/orders</code>", html);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "css", "main.css")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "js", "search.js")));
            Assert.Contains("List orders", File.ReadAllText(Path.Combine(outputDirectory, "search-index.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
