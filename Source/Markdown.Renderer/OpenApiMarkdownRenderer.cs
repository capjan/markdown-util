using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Markdown.Renderer;

public static class OpenApiMarkdownRenderer
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "PUT", "POST", "DELETE", "OPTIONS", "HEAD", "PATCH", "TRACE", "QUERY"
    };
    private static readonly Regex PathParameterRegex = new(@"\{[^{}]+\}", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, CachedDocument> Cache = new(
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);

    public static string ResolveSpecPath(string markdownPath, string specPath, string? allowedRootPath = null)
    {
        var fullSpecPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(markdownPath)!, specPath));
        if (allowedRootPath is not null)
        {
            var relativePath = Path.GetRelativePath(Path.GetFullPath(allowedRootPath), fullSpecPath);
            if (Path.IsPathRooted(relativePath) || relativePath == ".." ||
                relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"OpenAPI document '{specPath}' must stay inside the imported repository.");
        }

        return fullSpecPath;
    }

    public static string AppendFromFile(string markdown, string markdownPath, string specPath, string? allowedRootPath = null)
    {
        var fullSpecPath = ResolveSpecPath(markdownPath, specPath, allowedRootPath);
        var generatedMarkdown = RenderFile(fullSpecPath);
        var overviewTitle = Regex.Match(generatedMarkdown,
            @"\A## API overview \{\.openapi-overview\}\r?\n(?<title><p class=""openapi-api-title""><strong>(?<name>[^\r\n]+)</strong></p>\r?\n\r?\n)");
        if (overviewTitle.Success && Regex.Matches(markdown, @"(?m)^#\s+(.+?)\s*#*\r?$")
                .Cast<Match>().Any(heading => string.Equals(heading.Groups[1].Value.Trim(),
                    WebUtility.HtmlDecode(overviewTitle.Groups["name"].Value).Trim(), StringComparison.OrdinalIgnoreCase)))
            generatedMarkdown = generatedMarkdown.Remove(overviewTitle.Groups["title"].Index, overviewTitle.Groups["title"].Length);
        return string.IsNullOrWhiteSpace(markdown)
            ? generatedMarkdown
            : $"{markdown.TrimEnd()}\n\n{generatedMarkdown}";
    }

    private static string RenderFile(string path)
    {
        var extension = Path.GetExtension(path);
        var format = extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
                     extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)
            ? "yaml"
            : extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                ? "json"
                : throw new InvalidOperationException(
                    $"OpenAPI document '{path}' must use a .json, .yaml, or .yml extension.");

        try
        {
            var bytes = File.ReadAllBytes(path);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (Cache.TryGetValue(path, out var cached) && cached.Hash == hash) return cached.Markdown;

            using var stream = new MemoryStream(bytes);
            var settings = new OpenApiReaderSettings { LoadExternalRefs = false };
            settings.AddJsonReader();
            settings.AddYamlReader();
            var result = OpenApiDocument.LoadAsync(stream, format, settings).GetAwaiter().GetResult();
            var errors = result.Diagnostic?.Errors?.Select(error => error.Message).ToArray() ?? Array.Empty<string>();
            if (result.Document is null || errors.Length > 0)
                throw new InvalidOperationException($"Could not parse OpenAPI document '{path}': {string.Join("; ", errors)}");

            var markdown = Render(result.Document);
            Cache[path] = new CachedDocument(hash, markdown);
            return markdown;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OpenApiException or JsonException)
        {
            throw new InvalidOperationException($"Could not read OpenAPI document '{path}': {exception.Message}", exception);
        }
    }

    private static string Render(OpenApiDocument document)
    {
        var output = new StringBuilder();
        var info = document.Info;
        output.AppendLine("## API overview {.openapi-overview}");
        if (!string.IsNullOrWhiteSpace(info?.Title))
            AppendParagraph(output, $"<p class=\"openapi-api-title\"><strong>{HtmlEncoder.Default.Encode(info.Title)}</strong></p>");
        if (!string.IsNullOrWhiteSpace(info?.Version))
            AppendParagraph(output, $"<p class=\"openapi-api-version\">Version <code>{HtmlEncoder.Default.Encode(info.Version)}</code></p>");
        AppendMarkdown(output, info?.Description);

        // Raw HTML keeps the server list from merging into a list that ends the API description.
        var servers = (document.Servers ?? []).Where(server => !string.IsNullOrWhiteSpace(server.Url)).ToArray();
        if (servers.Length > 0)
        {
            output.Append("<div class=\"openapi-servers\"><span class=\"openapi-servers-label\">")
                .Append(servers.Length == 1 ? "Server" : "Servers")
                .Append("</span><ul>");
            foreach (var server in servers)
            {
                output.Append("<li><code>").Append(HtmlEncoder.Default.Encode(server.Url!)).Append("</code>");
                if (!string.IsNullOrWhiteSpace(server.Description))
                    output.Append("<span class=\"openapi-server-description\">")
                        .Append(HtmlEncoder.Default.Encode(server.Description))
                        .Append("</span>");
                output.Append("</li>");
            }
            output.AppendLine("</ul></div>");
            output.AppendLine();
        }

        var declaredTags = (document.Tags ?? new HashSet<OpenApiTag>()).Where(tag => !string.IsNullOrWhiteSpace(tag.Name)).ToArray();
        var tags = declaredTags.ToDictionary(tag => tag.Name!, tag => tag.Description ?? string.Empty, StringComparer.Ordinal);
        var tagOrder = declaredTags.Select(tag => tag.Name!).ToList();
        var operations = ReadOperations(document);
        foreach (var group in operations.GroupBy(operation => operation.Tag)
                     .OrderBy(group => TagOrder(tagOrder, group.Key)))
        {
            output.AppendLine($"## {CodeSpan(group.Key)} {{.openapi-tag}}");
            AppendMarkdown(output, tags.GetValueOrDefault(group.Key));
            foreach (var operation in group
                         .OrderBy(operation => OperationMethodOrder(operation.Method))
                         .ThenBy(operation => operation.Method, StringComparer.Ordinal)
                         .ThenBy(operation => operation.Path, StringComparer.Ordinal))
                AppendOperation(output, operation, document.Components?.Schemas);
        }

        return output.ToString();
    }

    private static List<Operation> ReadOperations(OpenApiDocument document)
    {
        var operations = new List<Operation>();
        foreach (var path in document.Paths)
        {
            var pathItem = ResolveReference(path.Value);
            foreach (var method in pathItem.Operations ?? [])
            {
                var tag = method.Value.Tags?.FirstOrDefault()?.Name ?? "Endpoints";
                var name = method.Key.Method.ToUpperInvariant();
                if (HttpMethods.Contains(name))
                    operations.Add(new Operation(path.Key, name, tag, pathItem, method.Value));
            }
        }

        return operations;
    }

    private static int OperationMethodOrder(string method) => method switch
    {
        "GET" => 0,
        "POST" => 1,
        "PUT" => 2,
        "PATCH" => 3,
        "DELETE" => 4,
        _ => 5
    };

    private static void AppendOperation(
        StringBuilder output,
        Operation operation,
        IDictionary<string, IOpenApiSchema>? schemas)
    {
        var pathParameterColors = GetPathParameterColors(operation.Path);
        output.AppendLine($"### {CodeSpan(operation.Method)} {PathMarkup(operation.Path, pathParameterColors)} {{.openapi-operation .openapi-method-{operation.Method.ToLowerInvariant()}}}");
        var summary = operation.Data.Summary;
        if (!string.IsNullOrWhiteSpace(summary))
            AppendParagraph(output, $"<p class=\"openapi-operation-summary\">{HtmlEncoder.Default.Encode(summary.Trim())}</p>");
        var operationId = operation.Data.OperationId;
        if (!string.IsNullOrWhiteSpace(operationId))
            AppendParagraph(output, $"<p class=\"openapi-operation-id\">Operation ID <code>{HtmlEncoder.Default.Encode(operationId)}</code></p>");
        AppendMarkdown(output, operation.Data.Description);

        AppendParameters(output, operation.PathItem, operation.Data, pathParameterColors, schemas);
        AppendRequestBody(output, operation.Data, schemas, $"{operation.Method}-{operation.Path}");
        AppendResponses(output, operation.Data, schemas, $"{operation.Method}-{operation.Path}");
    }

    private static void AppendParameters(
        StringBuilder output,
        IOpenApiPathItem pathItem,
        OpenApiOperation operation,
        IReadOnlyDictionary<string, string> pathParameterColors,
        IDictionary<string, IOpenApiSchema>? schemas)
    {
        var parameters = new Dictionary<string, IOpenApiParameter>(StringComparer.Ordinal);
        AddParameters(pathItem.Parameters ?? [], parameters);
        AddParameters(operation.Parameters ?? [], parameters);
        if (parameters.Count == 0) return;

        foreach (var group in parameters.Values
                     .Select(ResolveReference)
                     .GroupBy(parameter => parameter.In)
                     .OrderBy(group => ParameterLocationOrder(group.Key)))
        {
            output.AppendLine($"#### {ParameterHeading(group.Key)}");
            output.AppendLine("| Parameter | Description |");
            output.AppendLine("| --- | --- |");
            foreach (var parameter in group)
            {
                var name = parameter.Name ?? string.Empty;
                var nameMarkup = parameter.In == ParameterLocation.Path && pathParameterColors.TryGetValue(name, out var colorClass)
                    ? $"<span class=\"openapi-path-parameter {colorClass}\">{HtmlEncoder.Default.Encode(name)}</span>"
                    : HtmlEncoder.Default.Encode(name);
                if (parameter.Required) nameMarkup += RequiredMarker;
                output.AppendLine($"| {EscapeTableCell(DeclarationMarkup(nameMarkup, SchemaTypeMarkup(parameter.Schema), parameter.Schema, schemas))} | {EscapeTableCell(parameter.Description)} |");
            }
            output.AppendLine();
        }
    }

    private static int ParameterLocationOrder(ParameterLocation? location) => location switch
    {
        ParameterLocation.Path => 0,
        ParameterLocation.Query => 1,
        ParameterLocation.Header => 2,
        ParameterLocation.Cookie => 3,
        _ => 4
    };

    private static string ParameterHeading(ParameterLocation? location) => location switch
    {
        ParameterLocation.Path => "Path Parameters",
        ParameterLocation.Query => "Query Parameters",
        ParameterLocation.Header => "Header Parameters",
        ParameterLocation.Cookie => "Cookie Parameters",
        _ => $"{location?.ToString() ?? "Other"} Parameters"
    };

    private static void AddParameters(IEnumerable<IOpenApiParameter> values, IDictionary<string, IOpenApiParameter> parameters)
    {
        foreach (var value in values)
        {
            var parameter = ResolveReference(value);
            var key = $"{parameter.In}:{parameter.Name}";
            parameters[key] = parameter;
        }
    }

    private static void AppendRequestBody(
        StringBuilder output,
        OpenApiOperation operation,
        IDictionary<string, IOpenApiSchema>? schemas,
        string operationKey)
    {
        if (operation.RequestBody is null) return;
        var requestBody = ResolveReference(operation.RequestBody);
        output.AppendLine("#### Request body {.openapi-request-body}");
        if (requestBody.Required) output.AppendLine("Required.");
        AppendMarkdown(output, requestBody.Description);
        AppendMediaContent(
            output,
            requestBody.Content ?? new Dictionary<string, IOpenApiMediaType>(),
            schemas,
            $"{operationKey}-request",
            "openapi-request-media-type");
    }

    private static void AppendResponses(
        StringBuilder output,
        OpenApiOperation operation,
        IDictionary<string, IOpenApiSchema>? schemas,
        string operationKey)
    {
        if (operation.Responses is not { Count: > 0 }) return;
        foreach (var response in operation.Responses)
        {
            var responseObject = ResolveReference(response.Value);
            var statusName = ResponseStatusName(response.Key);
            var statusSuffix = string.IsNullOrWhiteSpace(statusName) ? string.Empty : $" - {EscapeMarkdown(statusName)}";
            output.AppendLine($"##### Response {CodeSpan(response.Key)}{statusSuffix} {{.openapi-response}}");
            AppendMarkdown(output, responseObject.Description);
            AppendResponseContent(
                output,
                responseObject.Content ?? new Dictionary<string, IOpenApiMediaType>(),
                schemas,
                $"{operationKey}-{response.Key}");
        }
    }

    private static void AppendResponseContent(
        StringBuilder output,
        IDictionary<string, IOpenApiMediaType> content,
        IDictionary<string, IOpenApiSchema>? schemas,
        string responseKey)
        => AppendMediaContent(output, content, schemas, responseKey, "openapi-media-type");

    private static void AppendMediaContent(
        StringBuilder output,
        IDictionary<string, IOpenApiMediaType> content,
        IDictionary<string, IOpenApiSchema>? schemas,
        string contentKey,
        string mediaHeadingClass)
    {
        foreach (var media in content)
        {
            var anchorPrefix = CreateSchemaAnchorPrefix($"{contentKey}-{media.Key}");
            output.AppendLine($"###### Media type {CodeSpan(media.Key)} {{.{mediaHeadingClass}}}");
            output.AppendLine("##### Example {.openapi-response-example}");
            AppendResponseSchemaName(output, media.Value.Schema, schemas);
            var example = media.Value.Example ?? (media.Value.Examples ?? new Dictionary<string, IOpenApiExample>()).Values
                .Select(ResolveReference)
                .Select(value => value.Value)
                .FirstOrDefault(value => value is not null);
            var exampleValue = example ?? (media.Value.Schema is null ? null : CreateSchemaExample(media.Value.Schema));
            if (exampleValue is null)
                output.AppendLine("No example is available.");
            else
                AppendCodeBlock(output, ExampleText(exampleValue, media.Key), ExampleLanguage(media.Key));

            output.AppendLine("##### Schema {.openapi-response-schema-table}");
            var flattenedArrayItemName = media.Value.Schema is null
                ? null
                : ArrayItemReferenceName(media.Value.Schema, schemas);
            var flattenedArrayItemAnchor = flattenedArrayItemName is null
                ? null
                : $"{anchorPrefix}-{SlugifySchemaName(flattenedArrayItemName)}";
            AppendResponseSchemaName(output, media.Value.Schema, schemas, flattenedArrayItemAnchor);
            AppendResponseSchemaTable(output, media.Value.Schema, schemas, anchorPrefix);
            AppendReferencedResponseSchemas(output, media.Value.Schema, schemas, anchorPrefix);

            output.AppendLine("##### Schema JSON {.openapi-response-schema-json}");
            AppendResponseSchemaName(output, media.Value.Schema, schemas);
            if (media.Value.Schema is null)
                output.AppendLine("No schema is defined.");
            else
            {
                AppendExample(output, SerializeSchema(media.Value.Schema));
                AppendReferencedResponseSchemaJson(output, media.Value.Schema, schemas);
            }
        }
    }

    private static string ExampleText(JsonNode example, string mediaType)
    {
        if (IsJsonMediaType(mediaType))
            return example.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        if (example is JsonValue value && value.TryGetValue<string>(out var text)) return text;
        return example.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ExampleLanguage(string mediaType)
    {
        if (IsJsonMediaType(mediaType)) return "json";
        if (mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)) return "xml";
        if (mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)) return "html";
        return string.Empty;
    }

    private static void AppendResponseSchemaName(
        StringBuilder output,
        IOpenApiSchema? schema,
        IDictionary<string, IOpenApiSchema>? components,
        string? anchor = null)
    {
        if (schema is null) return;
        if (anchor is not null)
            output.Append("<span id=\"").Append(HtmlEncoder.Default.Encode(anchor)).AppendLine("\"></span>");
        output.AppendLine($"**{CodeSpan(SchemaName(schema, components))}**");
        output.AppendLine();
        AppendMarkdown(output, SchemaDescription(schema, components));
    }

    private static string? SchemaDescription(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema>? components)
    {
        var resolved = ResolveResponseSchemaReference(schema, components);
        if (!string.IsNullOrWhiteSpace(resolved.Description)) return resolved.Description;
        return ArrayItemReferenceName(schema, components) is not null && resolved.Items is not null
            ? ResolveResponseSchemaReference(resolved.Items, components).Description
            : null;
    }

    private static void AppendResponseSchemaTable(
        StringBuilder output,
        IOpenApiSchema? schema,
        IDictionary<string, IOpenApiSchema>? components,
        string anchorPrefix)
    {
        if (schema is null)
        {
            output.AppendLine("No schema is defined.");
            return;
        }

        var tableSchema = ResolveResponseSchemaReference(schema, components);
        var isArrayOfReference = tableSchema.Type == JsonSchemaType.Array &&
                                 tableSchema.Items is not null &&
                                 GetReference(tableSchema.Items) is { IsLocal: true };
        if (isArrayOfReference) return;
        if (tableSchema.Items is not null && !isArrayOfReference)
            tableSchema = ResolveReference(tableSchema.Items);

        var properties = tableSchema.Properties ?? new Dictionary<string, IOpenApiSchema>();
        var required = tableSchema.Required ?? new HashSet<string>();
        var anchors = CreateSchemaAnchors(schema, components, anchorPrefix);
        output.AppendLine("| Property | Description |");
        output.AppendLine("| --- | --- |");
        if (properties.Count == 0)
        {
            output.AppendLine($"| {EscapeTableCell(DeclarationMarkup("value", SchemaTypeMarkup(schema), schema, components))} |  |");
            output.AppendLine();
            return;
        }

        AppendPropertyRows(output, properties, required, anchors, components);
        output.AppendLine();
    }

    private static void AppendPropertyRows(
        StringBuilder output,
        IDictionary<string, IOpenApiSchema> properties,
        ISet<string> required,
        IReadOnlyDictionary<string, string> anchors,
        IDictionary<string, IOpenApiSchema>? components)
    {
        foreach (var property in properties)
        {
            var propertySchema = ResolveReference(property.Value);
            var propertyName = HtmlEncoder.Default.Encode(property.Key) + (required.Contains(property.Key) ? RequiredMarker : string.Empty);
            output.AppendLine($"| {EscapeTableCell(DeclarationMarkup(propertyName, SchemaTypeMarkup(property.Value, anchors), property.Value, components))} | {EscapeTableCell(propertySchema.Description)} |");
        }
    }

    private static void AppendReferencedResponseSchemas(
        StringBuilder output,
        IOpenApiSchema? schema,
        IDictionary<string, IOpenApiSchema>? components,
        string anchorPrefix)
    {
        if (schema is null) return;
        var references = GetTableReferencedSchemas(schema, components);
        var flattenedArrayItemName = ArrayItemReferenceName(schema, components);
        var anchors = CreateSchemaAnchors(references.Keys, anchorPrefix);
        foreach (var (name, referencedSchema) in references)
        {
            var resolvedSchema = ResolveResponseSchemaReference(referencedSchema, components);
            if (name == flattenedArrayItemName)
            {
                if (resolvedSchema.Description != SchemaDescription(schema, components))
                    AppendMarkdown(output, resolvedSchema.Description);
                AppendSchemaPropertyTable(output, resolvedSchema, anchors, components);
                continue;
            }

            output.AppendLine($"###### {CodeSpan(name)} {{#{anchors[name]} .openapi-response-reference-schema}}");
            AppendMarkdown(output, resolvedSchema.Description);
            AppendSchemaPropertyTable(output, resolvedSchema, anchors, components);
        }
    }

    private static void AppendReferencedResponseSchemaJson(
        StringBuilder output,
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components)
    {
        foreach (var (name, referencedSchema) in GetReferencedResponseSchemas(schema, components))
        {
            var resolvedSchema = ResolveResponseSchemaReference(referencedSchema, components);
            // A paragraph instead of a heading keeps automatic heading ids out, so repeated tabs stay identical.
            AppendParagraph(output, $"**{CodeSpan(name)}**");
            AppendMarkdown(output, resolvedSchema.Description);
            AppendExample(output, SerializeSchema(resolvedSchema));
        }
    }

    private static Dictionary<string, IOpenApiSchema> GetReferencedResponseSchemas(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components)
    {
        var references = CollectReferencedSchemas(schema, components);
        if (GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } rootName })
            references.Remove(NormalizeSchemaReferenceName(rootName));
        return references;
    }

    private static string? ArrayItemReferenceName(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components)
    {
        var resolvedSchema = ResolveResponseSchemaReference(schema, components);
        return resolvedSchema.Type == JsonSchemaType.Array &&
               resolvedSchema.Items is not null &&
               GetReference(resolvedSchema.Items) is { IsLocal: true, Id: { Length: > 0 } id }
            ? NormalizeSchemaReferenceName(id)
            : null;
    }

    private static void AppendSchemaPropertyTable(
        StringBuilder output,
        IOpenApiSchema schema,
        IReadOnlyDictionary<string, string> anchors,
        IDictionary<string, IOpenApiSchema>? components)
    {
        var properties = schema.Properties ?? new Dictionary<string, IOpenApiSchema>();
        output.AppendLine("| Property | Description |");
        output.AppendLine("| --- | --- |");
        if (properties.Count == 0)
        {
            output.AppendLine($"| {EscapeTableCell(DeclarationMarkup("value", SchemaTypeMarkup(schema, anchors), schema, components))} |  |");
            output.AppendLine();
            return;
        }

        AppendPropertyRows(output, properties, schema.Required ?? new HashSet<string>(), anchors, components);
        output.AppendLine();
    }

    private static IReadOnlyDictionary<string, string> CreateSchemaAnchors(
        IOpenApiSchema? schema,
        IDictionary<string, IOpenApiSchema>? components,
        string anchorPrefix)
    {
        if (schema is null) return new Dictionary<string, string>();
        var references = GetTableReferencedSchemas(schema, components);
        return CreateSchemaAnchors(references.Keys, anchorPrefix);
    }

    // Enum values are listed on the rows that use them, so plain enums get no separate schema table.
    private static Dictionary<string, IOpenApiSchema> GetTableReferencedSchemas(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components)
    {
        var flattenedArrayItemName = ArrayItemReferenceName(schema, components);
        return GetReferencedResponseSchemas(schema, components)
            .Where(reference => reference.Key == flattenedArrayItemName ||
                                !IsEnumOnlySchema(ResolveResponseSchemaReference(reference.Value, components)))
            .ToDictionary(reference => reference.Key, reference => reference.Value, StringComparer.Ordinal);
    }

    private static bool IsEnumOnlySchema(IOpenApiSchema schema) =>
        schema.Enum is { Count: > 0 } && schema.Properties is not { Count: > 0 };

    private static IReadOnlyDictionary<string, string> CreateSchemaAnchors(
        IEnumerable<string> schemaNames,
        string anchorPrefix) => schemaNames.ToDictionary(
            name => name,
            name => $"{anchorPrefix}-{SlugifySchemaName(name)}",
            StringComparer.Ordinal);

    private static Dictionary<string, IOpenApiSchema> CollectReferencedSchemas(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components)
    {
        var references = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        Visit(schema);
        return references;

        void Visit(IOpenApiSchema current)
        {
            string? referenceName = null;
            if (GetReference(current) is { IsLocal: true, Id: { Length: > 0 } id })
            {
                referenceName = NormalizeSchemaReferenceName(id);
                references.TryAdd(referenceName, current);
            }

            current = ResolveResponseSchemaReference(current, components, referenceName);
            if (!visited.Add(current)) return;
            if (current.Items is not null) Visit(current.Items);
            if (current.AdditionalProperties is not null) Visit(current.AdditionalProperties);
            if (current.Not is not null) Visit(current.Not);
            foreach (var property in current.Properties?.Values ?? []) Visit(property);
            foreach (var nested in current.AllOf ?? []) Visit(nested);
            foreach (var nested in current.OneOf ?? []) Visit(nested);
            foreach (var nested in current.AnyOf ?? []) Visit(nested);
        }
    }

    private static IOpenApiSchema ResolveResponseSchemaReference(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema>? components,
        string? referenceName = null)
    {
        var resolved = ResolveReference(schema);
        if (!ReferenceEquals(resolved, schema)) return resolved;
        if (referenceName is null && GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } id })
            referenceName = NormalizeSchemaReferenceName(id);
        return referenceName is not null && components?.TryGetValue(referenceName, out var component) == true
            ? ResolveReference(component)
            : resolved;
    }

    private static string SchemaTypeMarkup(
        IOpenApiSchema? schema,
        IReadOnlyDictionary<string, string>? anchors = null)
    {
        if (schema is null) return TypeMarkup("any");
        if (GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } id })
        {
            var name = NormalizeSchemaReferenceName(id);
            var display = TypeMarkup(name);
            return anchors is not null && anchors.TryGetValue(name, out var anchor)
                ? $"<a class=\"openapi-type-reference\" href=\"#{HtmlEncoder.Default.Encode(anchor)}\">{display}</a>"
                : display;
        }

        schema = ResolveReference(schema);
        if (schema.Type?.HasFlag(JsonSchemaType.Array) == true && schema.Items is not null)
        {
            var array = $"{TypeMarkup("[")}{SchemaTypeMarkup(schema.Items, anchors)}{TypeMarkup("]")}";
            return schema.Type.Value.HasFlag(JsonSchemaType.Null) ? AppendOptionalTypeMarker(array) : array;
        }
        if (schema.OneOf is { Count: > 0 } nullableOneOf && HasNullableUnion(nullableOneOf))
        {
            var nonNullable = nullableOneOf.Single(nested => SchemaType(nested) != "null");
            return AppendOptionalTypeMarker(SchemaTypeMarkup(nonNullable, anchors));
        }
        if (schema.AnyOf is { Count: > 0 } nullableAnyOf && HasNullableUnion(nullableAnyOf))
        {
            var nonNullable = nullableAnyOf.Single(nested => SchemaType(nested) != "null");
            return AppendOptionalTypeMarker(SchemaTypeMarkup(nonNullable, anchors));
        }
        if (schema.OneOf is { Count: > 0 } oneOf)
            return string.Join(" or ", oneOf.Select(nested => SchemaTypeMarkup(nested, anchors)));
        if (schema.AnyOf is { Count: > 0 } anyOf)
            return string.Join(" or ", anyOf.Select(nested => SchemaTypeMarkup(nested, anchors)));
        if (schema.AllOf is { Count: > 0 } allOf)
            return string.Join(" & ", allOf.Select(nested => SchemaTypeMarkup(nested, anchors)));
        return TypeMarkup(SchemaType(schema));
    }

    private const string RequiredMarker =
        "<span class=\"openapi-required-marker\" role=\"img\" aria-label=\"Required\" data-tooltip=\"Required\"><svg viewBox=\"0 0 10 10\" aria-hidden=\"true\" focusable=\"false\"><path d=\"M5 1.2v7.6M1.7 3.1l6.6 3.8M8.3 3.1 1.7 6.9\"></path></svg></span>";

    private static string DeclarationMarkup(
        string name,
        string type,
        IOpenApiSchema? schema = null,
        IDictionary<string, IOpenApiSchema>? components = null)
    {
        var declaration = new StringBuilder("<span class=\"openapi-declaration\"><span class=\"openapi-declaration-name\">")
            .Append(name)
            .Append("</span><span class=\"openapi-declaration-colon\">:</span> <span class=\"openapi-declaration-type\">")
            .Append(type)
            .Append("</span>");
        var resolved = schema is null ? null : ResolveResponseSchemaReference(schema, components);
        if (resolved?.Default is { } defaultValue)
            declaration.Append("<span class=\"openapi-default\" data-tooltip=\"Default value\"> = ")
                .Append(HtmlEncoder.Default.Encode(defaultValue.ToJsonString()))
                .Append("</span>");
        declaration.Append("</span>");

        var values = schema is null ? null : EnumValues(schema, components);
        if (values is null) return declaration.ToString();
        declaration.Append("<span class=\"openapi-enum\" role=\"list\" aria-label=\"Allowed values\">");
        foreach (var value in values)
            declaration.Append("<code class=\"openapi-enum-value\" role=\"listitem\">")
                .Append(HtmlEncoder.Default.Encode(EnumValueText(value)))
                .Append("</code>");
        return declaration.Append("</span>").ToString();
    }

    private static IList<JsonNode>? EnumValues(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema>? components)
    {
        var resolved = ResolveResponseSchemaReference(schema, components);
        if (resolved.Enum is { Count: > 0 } values) return values;
        if (resolved.Type?.HasFlag(JsonSchemaType.Array) == true && resolved.Items is not null)
            return EnumValues(resolved.Items, components);
        var union = resolved.OneOf is { Count: > 0 } ? resolved.OneOf : resolved.AnyOf;
        return union is { Count: > 0 } && HasNullableUnion(union)
            ? EnumValues(union.Single(nested => SchemaType(nested) != "null"), components)
            : null;
    }

    private static string EnumValueText(JsonNode? value) =>
        value is JsonValue text && text.TryGetValue<string>(out var stringValue)
            ? stringValue
            : value?.ToJsonString() ?? "null";

    private static string TypeMarkup(string type)
    {
        var optional = type.EndsWith('?');
        var name = optional ? type[..^1] : type;
        var markup = $"<span class=\"openapi-type\"><span class=\"openapi-type-keyword\">{HtmlEncoder.Default.Encode(name)}</span>";
        return optional ? $"{markup}<span class=\"openapi-type-optional\">?</span></span>" : $"{markup}</span>";
    }

    private static string AppendOptionalTypeMarker(string markup) =>
        $"<span class=\"openapi-type\">{markup}<span class=\"openapi-type-optional\">?</span></span>";

    private static string SchemaName(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema>? components)
    {
        var resolvedSchema = ResolveResponseSchemaReference(schema, components);
        if (resolvedSchema.Type == JsonSchemaType.Array && resolvedSchema.Items is not null)
            return $"[{SchemaName(resolvedSchema.Items, components)}]";
        if (GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } id })
            return NormalizeSchemaReferenceName(id);
        return !string.IsNullOrWhiteSpace(resolvedSchema.Title) ? resolvedSchema.Title : SchemaType(resolvedSchema);
    }

    private static string CreateSchemaAnchorPrefix(string value)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..10].ToLowerInvariant();
        return $"openapi-response-schema-{hash}";
    }

    private static string NormalizeSchemaReferenceName(string name) =>
        name.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

    private static string SlugifySchemaName(string name)
    {
        var slug = new StringBuilder(name.Length);
        foreach (var character in name.ToLowerInvariant())
            if (char.IsAsciiLetterOrDigit(character)) slug.Append(character);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        return slug.ToString().Trim('-');
    }

    private static bool IsJsonMediaType(string mediaType)
    {
        var type = mediaType.Split(';', 2)[0].Trim();
        return type.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               type.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static string SerializeSchema(IOpenApiSchema schema)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text, new OpenApiJsonWriterSettings { Terse = false });
        ((OpenApiSchema)ResolveReference(schema)).SerializeAsV31(writer);
        text.Flush();
        return text.ToString();
    }

    private static JsonNode? CreateSchemaExample(IOpenApiSchema schema) =>
        CreateSchemaExample(schema, new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance));

    private static JsonNode? CreateSchemaExample(IOpenApiSchema schema, HashSet<IOpenApiSchema> ancestors)
    {
        schema = ResolveReference(schema);
        if (!ancestors.Add(schema)) return null;

        try
        {
            var suppliedExample = schema.Examples?.FirstOrDefault();
#pragma warning disable CS0618 // OpenAPI 3.0 still uses the singular schema example field.
            suppliedExample ??= schema.Example;
#pragma warning restore CS0618
            suppliedExample ??= schema.Default ?? schema.Enum?.FirstOrDefault();
            if (suppliedExample is not null) return suppliedExample.DeepClone();
            if (schema.OneOf?.FirstOrDefault() is { } oneOf) return CreateSchemaExample(oneOf, ancestors);
            if (schema.AnyOf?.FirstOrDefault() is { } anyOf) return CreateSchemaExample(anyOf, ancestors);

            if (schema.Type?.HasFlag(JsonSchemaType.Array) == true || schema.Items is not null)
            {
                var example = new JsonArray();
                if (schema.Items is not null) example.Add(CreateSchemaExample(schema.Items, ancestors));
                return example;
            }

            if (schema.AllOf is { Count: > 0 })
            {
                var example = new JsonObject();
                foreach (var part in schema.AllOf)
                {
                    if (CreateSchemaExample(part, ancestors) is not JsonObject partExample) continue;
                    foreach (var property in partExample)
                        example[property.Key] = property.Value?.DeepClone();
                }
                AddSchemaProperties(example, schema, ancestors);
                return example;
            }

            if (schema.Properties is { Count: > 0 } || schema.Type?.HasFlag(JsonSchemaType.Object) == true)
            {
                var example = new JsonObject();
                AddSchemaProperties(example, schema, ancestors);
                return example;
            }

            var type = schema.Type ?? JsonSchemaType.Null;
            if (type.HasFlag(JsonSchemaType.String)) return JsonValue.Create(StringExample(schema.Format));
            if (type.HasFlag(JsonSchemaType.Integer)) return JsonValue.Create(0);
            if (type.HasFlag(JsonSchemaType.Number)) return JsonValue.Create(0);
            if (type.HasFlag(JsonSchemaType.Boolean)) return JsonValue.Create(false);
            return null;
        }
        finally
        {
            ancestors.Remove(schema);
        }
    }

    private static string StringExample(string? format) => format?.ToLowerInvariant() switch
    {
        "uuid" => "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "date-time" => "2025-01-31T12:00:00Z",
        "date" => "2025-01-31",
        "time" => "12:00:00",
        "duration" => "PT30M",
        "email" => "user@example.com",
        "uri" or "url" => "https://example.com",
        "hostname" => "example.com",
        "ipv4" => "192.0.2.1",
        "ipv6" => "2001:db8::1",
        "byte" => "U3RyaW5n",
        _ => "string"
    };

    private static void AddSchemaProperties(JsonObject example, IOpenApiSchema schema, HashSet<IOpenApiSchema> ancestors)
    {
        foreach (var property in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
            example[property.Key] = CreateSchemaExample(property.Value, ancestors);
    }

    private static string? ResponseStatusName(string status)
    {
        var normalizedStatus = status.ToUpperInvariant();
        var name = normalizedStatus switch
        {
            "1XX" => "Informational",
            "2XX" => "Successful",
            "3XX" => "Redirection",
            "4XX" => "Client Error",
            "5XX" => "Server Error",
            "DEFAULT" => "Default",
            _ => null
        };

        if (name is not null) return name;
        if (int.TryParse(status, out var code) && code is >= 100 and <= 599)
        {
            using var response = new HttpResponseMessage((HttpStatusCode)code);
            return response.ReasonPhrase;
        }

        return null;
    }

    private static void AppendExample(StringBuilder output, string json)
        => AppendCodeBlock(output, json, "json");

    private static void AppendCodeBlock(StringBuilder output, string code, string language)
    {
        var fenceLength = Math.Max(3, LongestBacktickRun(code) + 1);
        var fence = new string('`', fenceLength);
        output.Append(fence).AppendLine(language);
        output.AppendLine(code);
        output.AppendLine(fence);
        output.AppendLine();
    }

    private static string SchemaType(IOpenApiSchema? schema)
    {
        if (schema is null) return "any";
        if (GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } id })
            return NormalizeSchemaReferenceName(id);

        schema = ResolveReference(schema);
        if (schema.OneOf is { Count: > 0 } oneOf) return SchemaUnionType(oneOf);
        if (schema.AnyOf is { Count: > 0 } anyOf) return SchemaUnionType(anyOf);
        if (schema.AllOf is { Count: > 0 } allOf) return string.Join(" & ", allOf.Select(SchemaType));

        var type = schema.Type is { } schemaType ? FormatSchemaType(schemaType) : null;
        if (type is null) type = schema.Properties is { Count: > 0 } ? "object" : "any";
        var optional = type.EndsWith('?');
        if (optional) type = type[..^1];
        // A format such as uuid, int64, or date-time names a primitive more precisely than its JSON type.
        var display = string.IsNullOrWhiteSpace(schema.Format)
            ? type
            : type is "string" or "integer" or "number"
                ? FormatName(schema.Format)
                : $"{type} ({schema.Format})";
        if (schema.Type == JsonSchemaType.Array && schema.Items is not null)
            display = $"array of {SchemaType(schema.Items)}";
        if (optional) display += "?";
        return display;
    }

    private static string FormatName(string format)
    {
        var parts = format.Trim().ToLowerInvariant().Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select((part, index) => index == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string SchemaUnionType(IEnumerable<IOpenApiSchema> schemas)
    {
        var types = schemas.Select(SchemaType).ToArray();
        return HasNullableUnion(types)
            ? $"{types.Single(type => type != "null")}?"
            : string.Join(" or ", types);
    }

    private static bool HasNullableUnion(IEnumerable<IOpenApiSchema> schemas) =>
        HasNullableUnion(schemas.Select(SchemaType).ToArray());

    private static bool HasNullableUnion(IReadOnlyCollection<string> types) =>
        types.Count == 2 && types.Count(type => type == "null") == 1;

    private static string FormatSchemaType(JsonSchemaType type)
    {
        if (type.HasFlag(JsonSchemaType.Null) && type != JsonSchemaType.Null)
        {
            var nonNullType = (type & ~JsonSchemaType.Null).ToString()
                .Replace(", ", " or ", StringComparison.Ordinal)
                .ToLowerInvariant();
            return $"{nonNullType}?";
        }

        return type.ToString().Replace(", ", " or ", StringComparison.Ordinal).ToLowerInvariant();
    }

    private static IOpenApiPathItem ResolveReference(IOpenApiPathItem item) =>
        item is IOpenApiReferenceHolder<OpenApiPathItem, IOpenApiPathItem, OpenApiReferenceWithDescriptionAndSummary> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static IOpenApiParameter ResolveReference(IOpenApiParameter item) =>
        item is IOpenApiReferenceHolder<OpenApiParameter, IOpenApiParameter, OpenApiReferenceWithDescription> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static IOpenApiRequestBody ResolveReference(IOpenApiRequestBody item) =>
        item is IOpenApiReferenceHolder<OpenApiRequestBody, IOpenApiRequestBody, OpenApiReferenceWithDescription> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static IOpenApiResponse ResolveReference(IOpenApiResponse item) =>
        item is IOpenApiReferenceHolder<OpenApiResponse, IOpenApiResponse, OpenApiReferenceWithDescriptionAndSummary> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static IOpenApiSchema ResolveReference(IOpenApiSchema item) =>
        item is IOpenApiReferenceHolder<OpenApiSchema, IOpenApiSchema, JsonSchemaReference> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static IOpenApiExample ResolveReference(IOpenApiExample item) =>
        item is IOpenApiReferenceHolder<OpenApiExample, IOpenApiExample, OpenApiReferenceWithDescriptionAndSummary> reference
            ? reference.RecursiveTarget ?? item
            : item;

    private static BaseOpenApiReference? GetReference(IOpenApiSchema item) =>
        item is IOpenApiReferenceHolder<OpenApiSchema, IOpenApiSchema, JsonSchemaReference> reference
            ? reference.Reference
            : null;

    private static int TagOrder(IReadOnlyList<string> tags, string tag)
    {
        for (var index = 0; index < tags.Count; index++)
            if (tags[index].Equals(tag, StringComparison.Ordinal)) return index;
        return int.MaxValue;
    }

    private static string EscapeMarkdown(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("*", "\\*", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal)
        .Replace("`", "&#96;", StringComparison.Ordinal);

    private static IReadOnlyDictionary<string, string> GetPathParameterColors(string path)
    {
        var colors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in PathParameterRegex.Matches(path))
        {
            var name = match.Value[1..^1];
            if (!colors.ContainsKey(name))
                colors.Add(name, $"openapi-path-parameter-color-{colors.Count % 6 + 1}");
        }
        return colors;
    }

    private static string PathMarkup(string path, IReadOnlyDictionary<string, string> pathParameterColors)
    {
        var output = new StringBuilder("<span class=\"openapi-path\">");
        var position = 0;
        foreach (Match match in PathParameterRegex.Matches(path))
        {
            output.Append(EncodePath(path[position..match.Index]));
            var name = match.Value[1..^1];
            var colorClass = pathParameterColors.TryGetValue(name, out var color)
                ? color
                : "openapi-path-parameter-color-1";
            output.Append($"<span class=\"openapi-path-parameter {colorClass}\">")
                .Append(EncodePath(match.Value))
                .Append("</span>");
            position = match.Index + match.Length;
        }

        output.Append(EncodePath(path[position..]));
        return output.Append("</span>").ToString();
    }

    private static string EncodePath(string value) => HtmlEncoder.Default.Encode(value)
        .Replace("{", "&#123;", StringComparison.Ordinal)
        .Replace("}", "&#125;", StringComparison.Ordinal);

    private static string EscapeTableCell(string? value) =>
        (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string CodeSpan(string value)
    {
        var fence = new string('`', Math.Max(1, LongestBacktickRun(value) + 1));
        var content = value.StartsWith('`') || value.EndsWith('`') ? $" {value} " : value;
        return $"{fence}{content}{fence}";
    }

    private static int LongestBacktickRun(string value)
    {
        var longest = 0;
        var current = 0;
        foreach (var character in value)
        {
            current = character == '`' ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }
        return longest;
    }

    private static void AppendParagraph(StringBuilder output, string paragraph)
    {
        output.AppendLine(paragraph);
        output.AppendLine();
    }

    private static void AppendMarkdown(StringBuilder output, string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;
        output.AppendLine(markdown.Trim());
        output.AppendLine();
    }

    private sealed record Operation(
        string Path,
        string Method,
        string Tag,
        IOpenApiPathItem PathItem,
        OpenApiOperation Data);

    private sealed record CachedDocument(string Hash, string Markdown);
}
