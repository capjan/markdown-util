using System.Text;
using Markdown.Renderer;

namespace Markdown.Renderer.Test;

public class OpenApiMarkdownRendererTest
{
    [Fact]
    public async Task RendersPathParametersAndNestedResponseMediaHeadings()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.1.0",
              "info": { "title": "Orders", "version": "1" },
              "paths": {
                "/orders/{id}": {
                  "get": {
                    "tags": ["Orders"],
                    "responses": {
                      "200": {
                        "description": "Found",
                        "content": { "application/json": { "schema": { "type": "string" } } }
                      }
                    }
                  }
                }
              }
            }
            """);

        var operationHeading = System.Text.RegularExpressions.Regex.Match(
            html,
            "<h3 id=\"(?<id>[^\"]+)\"><code>GET</code> <code>/orders/\\{id\\}</code>");
        Assert.True(operationHeading.Success, html);
        Assert.Contains($"href=\"#{operationHeading.Groups["id"].Value}\"", html);
        Assert.DoesNotContain("id=\"get-orders\" id=\"\"", html);
        Assert.Contains("<h5 id=\"response-200\">Response <code>200</code>", html);
        Assert.Contains("<h6 id=\"media-type-applicationjson\">Media type <code>application/json</code>", html);
    }

    [Fact]
    public async Task SeparatesAndEscapesOpenApiTitlesAndSummaries()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.1.0",
              "info": { "title": "Orders <script>", "version": "1.0" },
              "paths": {
                "/orders": {
                  "get": {
                    "tags": ["Orders"],
                    "summary": "Get `order` <details>",
                    "operationId": "getOrder",
                    "responses": { "204": { "description": "Done" } }
                  }
                }
              }
            }
            """);

        Assert.Contains("<p><strong>Orders &lt;script&gt;</strong></p>", html);
        Assert.Contains("<p>Version: <code>1.0</code></p>", html);
        Assert.Contains("<p><strong>Get `order` &lt;details&gt;</strong></p>", html);
        Assert.Contains("<p>Operation ID: <code>getOrder</code></p>", html);
        Assert.DoesNotContain("<p><strong>Orders <script>", html);
    }

    [Theory]
    [InlineData("`1.0")]
    [InlineData("1.0`")]
    public async Task RendersCodeSpansWhenValuesStartOrEndWithBackticks(string version)
    {
        var html = await RenderHtml($$"""
            {
              "openapi": "3.1.0",
              "info": { "title": "Orders", "version": "{{version}}" },
              "paths": {}
            }
            """);

        Assert.Contains($"<p>Version: <code>{version}</code></p>", html);
    }

    [Fact]
    public void ResolvesInternalReferencesAndRefreshesCachedSpecifications()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mdu-openapi-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var markdownPath = Path.Combine(directory, "orders.md");
        var specPath = Path.Combine(directory, "orders.json");
        try
        {
            File.WriteAllText(specPath, ReferencedSpecification("Order"));
            var firstRender = OpenApiMarkdownRenderer.AppendFromFile("# Orders", markdownPath, "orders.json");
            Assert.Contains("| id | path | Yes | string |", firstRender);
            Assert.Contains("#### Request body", firstRender);
            Assert.Contains("##### Response `201`", firstRender);
            Assert.Contains("### `Order`", firstRender);
            Assert.Contains("| id | string | Yes |", firstRender);

            File.WriteAllText(specPath, ReferencedSpecification("UpdatedOrder"));
            var secondRender = OpenApiMarkdownRenderer.AppendFromFile("# Orders", markdownPath, "orders.json");
            Assert.Contains("### `UpdatedOrder`", secondRender);
            Assert.DoesNotContain("### `Order`", secondRender);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<string> RenderHtml(string specification)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mdu-openapi-html-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var markdownPath = Path.Combine(directory, "orders.md");
            File.WriteAllText(Path.Combine(directory, "orders.json"), specification);
            var markdown = OpenApiMarkdownRenderer.AppendFromFile("# Orders", markdownPath, "orders.json");
            var output = new StringBuilder();
            using var writer = new StringWriter(output);
            await new HtmlRenderer().WriteHtml(writer, "Orders", string.Empty, 0, markdown, string.Empty);
            return output.ToString();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string ReferencedSpecification(string schemaName) => $$"""
        {
          "openapi": "3.1.0",
          "info": { "title": "Orders", "version": "1" },
          "paths": {
            "/orders": {
              "parameters": [ { "$ref": "#/components/parameters/id" } ],
              "post": {
                "tags": ["Orders"],
                "requestBody": { "$ref": "#/components/requestBodies/createOrder" },
                "responses": { "201": { "$ref": "#/components/responses/created" } }
              }
            }
          },
          "components": {
            "parameters": {
              "id": { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
            },
            "requestBodies": {
              "createOrder": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/{{schemaName}}" } } } }
            },
            "responses": {
              "created": { "description": "Created", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/{{schemaName}}" } } } }
            },
            "schemas": {
              "{{schemaName}}": {
                "type": "object",
                "required": ["id"],
                "properties": { "id": { "type": "string", "description": "Identifier" } }
              }
            }
          }
        }
        """;
}
