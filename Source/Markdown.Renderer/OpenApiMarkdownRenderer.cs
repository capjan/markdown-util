using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Markdown.Renderer;

public static class OpenApiMarkdownRenderer
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "PUT", "POST", "DELETE", "OPTIONS", "HEAD", "PATCH", "TRACE", "QUERY"
    };

    private static readonly ConcurrentDictionary<string, CachedDocument> Cache = new(
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);

    public static string ResolveSpecPath(string markdownPath, string specPath) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(markdownPath)!, specPath));

    public static string AppendFromFile(string markdown, string markdownPath, string specPath)
    {
        var fullSpecPath = ResolveSpecPath(markdownPath, specPath);
        var generatedMarkdown = RenderFile(fullSpecPath);
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
        if (!string.IsNullOrWhiteSpace(info?.Title)) AppendParagraph(output, $"**{EscapeMarkdown(info.Title)}**");
        if (!string.IsNullOrWhiteSpace(info?.Version)) AppendParagraph(output, $"Version: {CodeSpan(info.Version)}");
        AppendMarkdown(output, info?.Description);

        foreach (var server in document.Servers ?? [])
            if (!string.IsNullOrWhiteSpace(server.Url)) output.AppendLine($"- Server: {CodeSpan(server.Url)}");
        if (document.Servers is { Count: > 0 }) output.AppendLine();

        var declaredTags = (document.Tags ?? new HashSet<OpenApiTag>()).Where(tag => !string.IsNullOrWhiteSpace(tag.Name)).ToArray();
        var tags = declaredTags.ToDictionary(tag => tag.Name!, tag => tag.Description ?? string.Empty, StringComparer.Ordinal);
        var tagOrder = declaredTags.Select(tag => tag.Name!).ToList();
        var operations = ReadOperations(document);
        foreach (var group in operations.GroupBy(operation => operation.Tag)
                     .OrderBy(group => TagOrder(tagOrder, group.Key)))
        {
            output.AppendLine($"## {CodeSpan(group.Key)} {{.openapi-tag}}");
            AppendMarkdown(output, tags.GetValueOrDefault(group.Key));
            foreach (var operation in group)
                AppendOperation(output, operation);
        }

        AppendSchemas(output, document);
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

    private static void AppendOperation(StringBuilder output, Operation operation)
    {
        output.AppendLine($"### {CodeSpan(operation.Method)} {CodeHtml(operation.Path)} {{.openapi-operation .openapi-method-{operation.Method.ToLowerInvariant()}}}");
        var summary = operation.Data.Summary;
        if (!string.IsNullOrWhiteSpace(summary)) AppendParagraph(output, $"**{EscapeMarkdown(summary)}**");
        var operationId = operation.Data.OperationId;
        if (!string.IsNullOrWhiteSpace(operationId)) AppendParagraph(output, $"Operation ID: {CodeSpan(operationId)}");
        AppendMarkdown(output, operation.Data.Description);

        AppendParameters(output, operation.PathItem, operation.Data);
        AppendRequestBody(output, operation.Data);
        AppendResponses(output, operation.Data);
    }

    private static void AppendParameters(StringBuilder output, IOpenApiPathItem pathItem, OpenApiOperation operation)
    {
        var parameters = new Dictionary<string, IOpenApiParameter>(StringComparer.Ordinal);
        AddParameters(pathItem.Parameters ?? [], parameters);
        AddParameters(operation.Parameters ?? [], parameters);
        if (parameters.Count == 0) return;

        output.AppendLine("#### Parameters");
        output.AppendLine("| Name | In | Required | Type | Description |");
        output.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var value in parameters.Values)
        {
            var parameter = ResolveReference(value);
            var name = parameter.Name ?? string.Empty;
            var location = parameter.In?.ToString().ToLowerInvariant() ?? string.Empty;
            var required = parameter.Required ? "Yes" : "No";
            output.AppendLine($"| {EscapeTableCell(name)} | {EscapeTableCell(location)} | {required} | {EscapeTableCell(SchemaType(parameter.Schema))} | {EscapeTableCell(parameter.Description)} |");
        }
        output.AppendLine();
    }

    private static void AddParameters(IEnumerable<IOpenApiParameter> values, IDictionary<string, IOpenApiParameter> parameters)
    {
        foreach (var value in values)
        {
            var parameter = ResolveReference(value);
            var key = $"{parameter.In}:{parameter.Name}";
            parameters[key] = parameter;
        }
    }

    private static void AppendRequestBody(StringBuilder output, OpenApiOperation operation)
    {
        if (operation.RequestBody is null) return;
        var requestBody = ResolveReference(operation.RequestBody);
        output.AppendLine("#### Request body");
        if (requestBody.Required) output.AppendLine("Required.");
        AppendMarkdown(output, requestBody.Description);
        AppendContent(output, requestBody.Content ?? new Dictionary<string, IOpenApiMediaType>(), mediaHeadingLevel: 5);
    }

    private static void AppendResponses(StringBuilder output, OpenApiOperation operation)
    {
        if (operation.Responses is not { Count: > 0 }) return;
        output.AppendLine("#### Responses");
        foreach (var response in operation.Responses)
        {
            var responseObject = ResolveReference(response.Value);
            output.AppendLine($"##### Response {CodeSpan(response.Key)}");
            AppendMarkdown(output, responseObject.Description);
            AppendContent(output, responseObject.Content ?? new Dictionary<string, IOpenApiMediaType>(), mediaHeadingLevel: 6);
        }
    }

    private static void AppendContent(
        StringBuilder output,
        IDictionary<string, IOpenApiMediaType> content,
        int mediaHeadingLevel)
    {
        foreach (var media in content)
        {
            output.AppendLine($"{new string('#', mediaHeadingLevel)} Media type {CodeSpan(media.Key)}");
            if (media.Value.Schema is not null)
                output.AppendLine($"Schema: {CodeSpan(SchemaType(media.Value.Schema))}");

            var example = media.Value.Example ?? (media.Value.Examples ?? new Dictionary<string, IOpenApiExample>()).Values
                .Select(ResolveReference)
                .Select(value => value.Value)
                .FirstOrDefault(value => value is not null);
            if (example is not null) AppendExample(output, example.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void AppendExample(StringBuilder output, string json)
    {
        var fenceLength = Math.Max(3, LongestBacktickRun(json) + 1);
        var fence = new string('`', fenceLength);
        output.AppendLine($"{fence}json");
        output.AppendLine(json);
        output.AppendLine(fence);
        output.AppendLine();
    }

    private static void AppendSchemas(StringBuilder output, OpenApiDocument document)
    {
        var schemas = document.Components?.Schemas;
        if (schemas is null || schemas.Count == 0) return;

        output.AppendLine("## Schemas {.openapi-schemas}");
        foreach (var entry in schemas)
        {
            var schema = ResolveReference(entry.Value);
            output.AppendLine($"### {CodeSpan(entry.Key)} {{.openapi-schema}}");
            output.AppendLine($"Type: {CodeSpan(SchemaType(entry.Value))}");
            AppendMarkdown(output, schema.Description);

            var properties = schema.Properties ?? new Dictionary<string, IOpenApiSchema>();
            if (properties.Count == 0) continue;
            var required = schema.Required ?? new HashSet<string>();
            output.AppendLine("| Property | Type | Required | Description |");
            output.AppendLine("| --- | --- | --- | --- |");
            foreach (var property in properties)
            {
                var isRequired = required.Contains(property.Key) ? "Yes" : "No";
                var propertySchema = ResolveReference(property.Value);
                output.AppendLine($"| {EscapeTableCell(property.Key)} | {EscapeTableCell(SchemaType(property.Value))} | {isRequired} | {EscapeTableCell(propertySchema.Description)} |");
            }
            output.AppendLine();
        }
    }

    private static string SchemaType(IOpenApiSchema? schema)
    {
        if (schema is null) return "any";
        if (GetReference(schema) is { IsLocal: true, Id: { Length: > 0 } id })
            return id.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

        schema = ResolveReference(schema);
        if (schema.OneOf is { Count: > 0 } oneOf) return string.Join(" or ", oneOf.Select(SchemaType));
        if (schema.AnyOf is { Count: > 0 } anyOf) return string.Join(" or ", anyOf.Select(SchemaType));
        if (schema.AllOf is { Count: > 0 } allOf) return string.Join(" & ", allOf.Select(SchemaType));

        var type = schema.Type?.ToString().Replace(", ", " or ", StringComparison.Ordinal).ToLowerInvariant();
        if (type is null) type = schema.Properties is { Count: > 0 } ? "object" : "any";
        var display = string.IsNullOrWhiteSpace(schema.Format) ? type : $"{type} ({schema.Format})";
        if (schema.Type == JsonSchemaType.Array && schema.Items is not null)
            display = $"array of {SchemaType(schema.Items)}";
        if (schema.Enum is { Count: > 0 } values)
            display += $": {string.Join(", ", values.Select(item => item?.ToString()))}";
        return display;
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

    private static string CodeHtml(string value) =>
        $"<code>{HtmlEncoder.Default.Encode(value).Replace("{", "&#123;", StringComparison.Ordinal).Replace("}", "&#125;", StringComparison.Ordinal)}</code>";

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
