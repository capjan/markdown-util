using System.Text;
using Markdown.Renderer;

namespace Markdown.Renderer.Test;

public class OpenApiMarkdownRendererTest
{
    [Fact]
    public async Task CollapsesEndpointsAndRendersResponseMediaTypesAndTabs()
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
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "object",
                              "required": ["id"],
                              "properties": { "id": { "type": "integer", "description": "Order identifier" } }
                            },
                            "example": { "id": 42 }
                          },
                          "text/plain": { "schema": { "type": "string" }, "example": "Found" }
                        }
                      },
                      "404": { "description": "Not found" }
                    }
                  }
                }
              }
            }
            """);

        var operationHeading = System.Text.RegularExpressions.Regex.Match(
            html,
            "<summary(?<attributes>[^>]*)>\\s*<code>GET</code> <code>/orders/\\{id\\}</code>");
        Assert.True(operationHeading.Success, html);
        Assert.Contains("class=\"openapi-operation\"", operationHeading.Groups["attributes"].Value);
        Assert.Contains("<details class=\"openapi-endpoint openapi-method-get\">", html);
        var operationId = System.Text.RegularExpressions.Regex.Match(
            operationHeading.Groups["attributes"].Value,
            "id=\"(?<id>[^\"]+)\"").Groups["id"].Value;
        Assert.Contains($"href=\"#{operationId}\"", html);
        Assert.DoesNotContain("<details class=\"openapi-endpoint openapi-method-get\" open", html);
        var detailsStart = html.IndexOf("<details class=\"openapi-endpoint openapi-method-get\">", StringComparison.Ordinal);
        var detailsEnd = html.IndexOf("</details>", detailsStart, StringComparison.Ordinal);
        var responsePanel = html.IndexOf("<section class=\"openapi-response-panel openapi-status-2xx\" data-response-status=\"200\">", detailsStart, StringComparison.Ordinal);
        Assert.True(detailsEnd > responsePanel, html);
        Assert.DoesNotContain("id=\"get-orders\" id=\"\"", html);
        Assert.Contains("<div class=\"openapi-response-heading\">Response <span class=\"openapi-response-position\">1 of 2</span></div>", html);
        Assert.DoesNotContain("2 Responses", html);
        Assert.Contains("<option value=\"200\" data-response-position=\"1\" class=\"openapi-status-2xx\" selected>200 - OK</option>", html);
        Assert.Contains("<option value=\"404\" data-response-position=\"2\" class=\"openapi-status-4xx\">404 - Not Found</option>", html);
        Assert.DoesNotContain("Response <code>200</code>", html);
        Assert.Contains("Media Type <span class=\"openapi-media-type-position\">1 of 2</span>", html);
        Assert.Contains("<option value=\"0\" selected>application/json</option>", html);
        Assert.Contains("<option value=\"1\">text/plain</option>", html);
        Assert.Contains("<button type=\"button\" class=\"openapi-response-tab\" role=\"tab\"", html);
        Assert.Contains(">Example</button>", html);
        Assert.Contains(">Schema</button>", html);
        Assert.Contains(">Schema JSON</button>", html);
        Assert.Equal(6, System.Text.RegularExpressions.Regex.Matches(html, "role=\"tab\"").Count);
        Assert.Contains("<th>Property</th>", html);
        Assert.DoesNotContain("<th>Required</th>", html);
        Assert.Contains("<span class=\"openapi-required-marker\" aria-label=\"Required\">*</span>", html);
        Assert.Contains("<td>Order identifier</td>", html);
        Assert.Contains("<div class=\"openapi-response-description\">", html);
        Assert.True(
            html.IndexOf("class=\"openapi-response-select", StringComparison.Ordinal) <
            html.IndexOf("class=\"openapi-response-description", StringComparison.Ordinal) &&
            html.IndexOf("class=\"openapi-response-description", StringComparison.Ordinal) <
            html.IndexOf("class=\"openapi-media-type-select", StringComparison.Ordinal) &&
            html.IndexOf("class=\"openapi-media-type-select", StringComparison.Ordinal) <
            html.IndexOf("role=\"tablist\"", StringComparison.Ordinal), html);
        Assert.Contains("&quot;type&quot;", html);
        Assert.Contains("&quot;properties&quot;", html);
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
                    "responses": {
                      "204": {
                        "description": "Done",
                        "content": { "text/plain": { "schema": { "type": "string" } } }
                      }
                    }
                  }
                }
              }
            }
            """);

        Assert.Contains("<p><strong>Orders &lt;script&gt;</strong></p>", html);
        Assert.Contains("<p>Version: <code>1.0</code></p>", html);
        Assert.Contains("<p><strong>Get `order` &lt;details&gt;</strong></p>", html);
        Assert.Contains("<p>Operation ID: <code>getOrder</code></p>", html);
        Assert.Contains("Response <span class=\"openapi-response-position\">1 of 1</span>", html);
        Assert.Contains("Media Type <span class=\"openapi-media-type-position\">1 of 1</span>", html);
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
            Assert.Contains("| id<span class=\"openapi-required-marker\" aria-label=\"Required\">*</span> | string | Identifier |", firstRender);

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
