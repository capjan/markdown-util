using System.Text;
using Markdown.Renderer;

namespace Markdown.Renderer.Test;

public class OpenApiMarkdownRendererTest
{
    [Fact]
    public async Task RendersNullableArrayItemsAsLinkedCompactTypes()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.1.0",
              "info": { "title": "Locations", "version": "1" },
              "paths": { "/locations": { "get": { "responses": {
                "200": { "description": "Locations", "content": { "application/json": {
                  "schema": { "type": "object", "properties": {
                    "metaInfoAssets": { "type": ["null", "array"], "items": { "$ref": "#/components/schemas/LocationAssetDto" } }
                  } }
                } } }
              } } } },
              "components": { "schemas": { "LocationAssetDto": {
                "type": "object", "properties": { "id": { "type": "string" } }
              } } }
            }
            """);
        var cell = System.Text.RegularExpressions.Regex.Match(html,
            @"<td>(?<type><span[^>]+><span[^>]+>metaInfoAssets.*?)</td>").Groups["type"].Value;
        Assert.Equal("metaInfoAssets: [LocationAssetDto]?", System.Text.RegularExpressions.Regex.Replace(cell, "<[^>]+>", ""));
        var link = System.Text.RegularExpressions.Regex.Match(cell,
            "class=\"openapi-type-reference\" href=\"#(?<anchor>[^\"]+)\"");
        Assert.True(link.Success);
        Assert.Contains($"id=\"{link.Groups["anchor"].Value}\"", html);
        Assert.Contains("class=\"openapi-type-optional\">?</span>", cell);
        Assert.DoesNotContain("class=\"openapi-description-toggle\"", html);
    }

    [Fact]
    public async Task CollapsesEndpointsAndRendersResponseMediaTypesAndTabs()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.1.0",
              "info": { "title": "Orders", "version": "1" },
              "tags": [{ "name": "Orders", "description": "Order operations" }],
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
            "<summary(?<attributes>[^>]*)>\\s*<code>GET</code> <span class=\"openapi-path\">/orders/<span class=\"openapi-path-parameter openapi-path-parameter-color-1\">\\{id\\}</span></span>");
        Assert.True(operationHeading.Success, html);
        Assert.Contains("class=\"openapi-operation\"", operationHeading.Groups["attributes"].Value);
        Assert.Contains("<details class=\"openapi-endpoint openapi-method-get\">", html);
        Assert.Contains("<span class=\"openapi-tag-title\"><code>Orders</code></span>", html);
        Assert.Contains("<span class=\"openapi-tag-description\">Order operations</span>", html);
        Assert.Contains("class=\"openapi-tag-method-count openapi-method-get\" data-method=\"openapi-method-get\" data-count=\"1\" data-tooltip=\"1 GET endpoint\" aria-label=\"1 GET endpoint\">1", html);
        var operationId = System.Text.RegularExpressions.Regex.Match(
            operationHeading.Groups["attributes"].Value,
            "id=\"(?<id>[^\"]+)\"").Groups["id"].Value;
        Assert.Contains($"id=\"{operationId}\"", html);
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
        Assert.Contains("<th class=\"openapi-description-header\"><span class=\"openapi-schema-table-header\"><span class=\"openapi-description-label\">Description</span><button type=\"button\" class=\"openapi-description-toggle\" aria-expanded=\"false\">Show descriptions</button></span></th>", html);
        Assert.Contains("<div class=\"openapi-schema-table\">", html);
        Assert.DoesNotContain("<th>Type</th>", html);
        Assert.DoesNotContain("<th>Required</th>", html);
        Assert.Contains("<span class=\"openapi-required-marker\" role=\"img\" aria-label=\"Required\" data-tooltip=\"Required\">", html);
        Assert.DoesNotContain("title=\"Required\"", html);
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
        Assert.DoesNotContain("class=\"openapi-schema\"", html);
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

        Assert.Contains("<p class=\"openapi-api-title\"><strong>Orders &lt;script&gt;</strong></p>", html);
        Assert.Contains("<p class=\"openapi-api-version\">Version <code>1.0</code></p>", html);
        Assert.Contains("<span class=\"openapi-operation-summary\">Get `order` &lt;details&gt;</span></summary>", html);
        Assert.Contains("<p class=\"openapi-operation-id\">Operation ID <code>getOrder</code></p>", html);
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

        Assert.Contains($"<p class=\"openapi-api-version\">Version <code>{version}</code></p>", html);
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
            Assert.Contains("#### Path Parameters", firstRender);
            Assert.DoesNotContain("openapi-api-title", firstRender);
            Assert.Contains("<span class=\"openapi-path-parameter openapi-path-parameter-color-1\">id</span>", firstRender);
            var plainMarkdown = System.Text.RegularExpressions.Regex.Replace(firstRender, "<[^>]+>", "");
            Assert.Contains("| id: string |", plainMarkdown);
            Assert.Contains("id<span class=\"openapi-required-marker\"", firstRender);
            Assert.Contains("#### Request body", firstRender);
            Assert.Contains("##### Response `201`", firstRender);
            Assert.Contains("**`Order`**", firstRender);
            Assert.Contains($"**`Order`**{Environment.NewLine}{Environment.NewLine}Order description.", firstRender);
            Assert.Contains("| id: string | Identifier |", plainMarkdown);
            Assert.DoesNotContain("## Schemas", firstRender);

            File.WriteAllText(specPath, ReferencedSpecification("UpdatedOrder"));
            var secondRender = OpenApiMarkdownRenderer.AppendFromFile("# Orders", markdownPath, "orders.json");
            Assert.Contains("**`UpdatedOrder`**", secondRender);
            Assert.DoesNotContain("**`Order`**", secondRender);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ListsEnumValuesAndDefaultsOnTheRowsThatUseThem()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Features", "version": "1" },
              "servers": [{ "url": "https://api.example.com", "description": "Production" }],
              "paths": { "/features": { "get": {
                "parameters": [
                  { "name": "IsActive", "in": "query", "schema": { "$ref": "#/components/schemas/ActiveSearchOption" } },
                  { "name": "Limit", "in": "query", "required": true, "schema": { "type": "integer", "format": "int32", "default": 20 } }
                ],
                "responses": { "200": { "description": "Features", "content": { "application/json": { "schema": {
                  "type": "object", "properties": {
                    "contactType": { "$ref": "#/components/schemas/ContactType" },
                    "length": { "type": "string", "format": "date-span" },
                    "file": { "type": "string", "format": "binary" },
                    "size": { "type": "integer", "format": "int64" }
                  }
                } } } } }
              } } },
              "components": { "schemas": {
                "ActiveSearchOption": { "type": "string", "enum": ["NonActive", "Active"] },
                "ContactType": { "type": "string", "description": "Contact type.", "enum": ["Request", "All"] }
              } }
            }
            """);

        var plain = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        Assert.Contains("<span class=\"openapi-enum\" role=\"list\" aria-label=\"Allowed values\"><code class=\"openapi-enum-value\" role=\"listitem\">NonActive</code><code class=\"openapi-enum-value\" role=\"listitem\">Active</code></span>", html);
        Assert.Contains("<code class=\"openapi-enum-value\" role=\"listitem\">Request</code>", html);
        Assert.Contains("<span class=\"openapi-default\" data-tooltip=\"Default value\"> = 20</span>", html);
        Assert.Contains("length: dateSpan", plain);
        Assert.Contains("file: binary", plain);
        Assert.Contains("size: int64", plain);
        Assert.DoesNotContain("string: Request", plain);
        Assert.DoesNotContain("openapi-response-reference-schema", html);
        Assert.DoesNotContain("class=\"openapi-type-reference\"", html);
        Assert.Contains("<div class=\"openapi-servers\"><span class=\"openapi-servers-label\">Server</span><ul><li><code>https://api.example.com</code><span class=\"openapi-server-description\">Production</span></li></ul></div>", html);
    }

    [Fact]
    public async Task EmitsRepeatedTabContentsOnceWithScopedAnchors()
    {
        var html = await RenderHtml("""
            {
              "openapi": "3.1.0",
              "info": { "title": "Orders", "version": "1" },
              "paths": { "/orders": { "get": { "responses": {
                "200": { "description": "Found", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Order" } } } },
                "400": { "description": "Bad", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Problem" } } } },
                "500": { "description": "Error", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Problem" } } } }
              } } } },
              "components": { "schemas": {
                "Order": { "type": "object", "properties": { "id": { "type": "string" } } },
                "Problem": { "type": "object", "properties": { "detail": { "$ref": "#/components/schemas/Detail" } } },
                "Detail": { "type": "object", "properties": { "title": { "type": "string" } } }
              } }
            }
            """);

        var templates = System.Text.RegularExpressions.Regex.Matches(html, "<template data-openapi-shared=\"(?<id>[^\"]+)\">");
        Assert.Equal(3, templates.Count);
        var placeholders = System.Text.RegularExpressions.Regex.Matches(html,
            "<div class=\"openapi-shared-content\" data-openapi-shared=\"(?<id>[^\"]+)\" data-openapi-scope=\"(?<scope>[^\"]*)\"></div>");
        Assert.Equal(6, placeholders.Count);
        var schemaScopes = placeholders.Select(match => match.Groups["scope"].Value).Where(scope => scope.Length > 0).Distinct().ToArray();
        Assert.Equal(2, schemaScopes.Length);
        Assert.All(schemaScopes, scope => Assert.StartsWith("openapi-response-schema-", scope));
        Assert.Contains("href=\"#openapi-shared-scope-detail\"", html);
        Assert.DoesNotContain("openapi-shared-->", html);
        Assert.Contains("<td><span class=\"openapi-declaration\"><span class=\"openapi-declaration-name\">id</span>", html);
        Assert.Contains("template[data-openapi-shared=", html);
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
            "/orders/{id}": {
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
                "description": "Order description.",
                "required": ["id"],
                "properties": { "id": { "type": "string", "description": "Identifier" } }
              }
            }
          }
        }
        """;
}
