using System.Text;
using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Markdown.Renderer;

public static class OpenApiMarkdownRenderer
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace", "query"
    };

    public static string AppendFromFile(string markdown, string markdownPath, string specPath)
    {
        var fullSpecPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(markdownPath)!, specPath));
        var generatedMarkdown = RenderFile(fullSpecPath);
        return string.IsNullOrWhiteSpace(markdown)
            ? generatedMarkdown
            : $"{markdown.TrimEnd()}\n\n{generatedMarkdown}";
    }

    private static string RenderFile(string path)
    {
        var settings = new OpenApiReaderSettings { LoadExternalRefs = false };
        settings.AddJsonReader();
        settings.AddYamlReader();

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
            using var stream = File.OpenRead(path);
            var result = OpenApiDocument.LoadAsync(stream, format, settings).GetAwaiter().GetResult();
            var errors = result.Diagnostic?.Errors?.Select(error => error.Message).ToArray() ?? Array.Empty<string>();
            if (result.Document is null || errors.Length > 0)
            {
                throw new InvalidOperationException($"Could not parse OpenAPI document '{path}': {string.Join("; ", errors)}");
            }

            var json = result.Document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_2).GetAwaiter().GetResult();
            using var document = JsonDocument.Parse(json);
            return Render(document.RootElement);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OpenApiException or JsonException)
        {
            throw new InvalidOperationException($"Could not read OpenAPI document '{path}': {exception.Message}", exception);
        }
    }

    private static string Render(JsonElement document)
    {
        var output = new StringBuilder();
        var info = GetObject(document, "info");
        var title = GetString(info, "title");
        var version = GetString(info, "version");
        var description = GetString(info, "description");

        output.AppendLine("## API overview");
        if (!string.IsNullOrWhiteSpace(title)) output.AppendLine($"**{EscapeMarkdown(title)}**");
        if (!string.IsNullOrWhiteSpace(version)) output.AppendLine($"Version: {CodeSpan(version)}");
        AppendMarkdown(output, description);

        if (TryGet(document, "servers", out var servers) && servers.ValueKind == JsonValueKind.Array)
        {
            foreach (var server in servers.EnumerateArray())
            {
                var url = GetString(server, "url");
                if (!string.IsNullOrWhiteSpace(url)) output.AppendLine($"- Server: {CodeSpan(url)}");
            }
            output.AppendLine();
        }

        var tagDescriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        var tagOrder = new List<string>();
        if (TryGet(document, "tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tags.EnumerateArray())
            {
                var name = GetString(tag, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                tagOrder.Add(name);
                tagDescriptions[name] = GetString(tag, "description") ?? string.Empty;
            }
        }

        var operations = ReadOperations(document);
        foreach (var group in operations.GroupBy(operation => operation.Tag)
                     .OrderBy(group => TagOrder(tagOrder, group.Key)))
        {
            output.AppendLine($"## {CodeSpan(group.Key)}");
            AppendMarkdown(output, tagDescriptions.GetValueOrDefault(group.Key));
            foreach (var operation in group)
                AppendOperation(output, operation, document);
        }

        AppendSchemas(output, document);
        return output.ToString();
    }

    private static List<Operation> ReadOperations(JsonElement document)
    {
        var operations = new List<Operation>();
        if (!TryGet(document, "paths", out var paths) || paths.ValueKind != JsonValueKind.Object) return operations;

        foreach (var path in paths.EnumerateObject())
        {
            var pathItem = ResolveReference(path.Value, document);
            if (pathItem.ValueKind != JsonValueKind.Object) continue;
            foreach (var method in pathItem.EnumerateObject())
            {
                if (!HttpMethods.Contains(method.Name) || method.Value.ValueKind != JsonValueKind.Object) continue;
                var tag = GetArray(method.Value, "tags") is { } tags
                    ? tags.EnumerateArray().Select(value => value.GetString())
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "Endpoints"
                    : "Endpoints";
                operations.Add(new Operation(path.Name, method.Name.ToUpperInvariant(), tag, pathItem, method.Value));
            }
        }

        return operations;
    }

    private static void AppendOperation(StringBuilder output, Operation operation, JsonElement document)
    {
        output.AppendLine($"### {CodeSpan(operation.Method)} {CodeSpan(operation.Path)}");
        var summary = GetString(operation.Data, "summary");
        if (!string.IsNullOrWhiteSpace(summary)) output.AppendLine($"**{EscapeMarkdown(summary)}**");
        var operationId = GetString(operation.Data, "operationId");
        if (!string.IsNullOrWhiteSpace(operationId)) output.AppendLine($"Operation ID: {CodeSpan(operationId)}");
        AppendMarkdown(output, GetString(operation.Data, "description"));
        if (!string.IsNullOrWhiteSpace(summary) || !string.IsNullOrWhiteSpace(operationId)) output.AppendLine();

        AppendParameters(output, operation.PathItem, operation.Data, document);
        AppendRequestBody(output, operation.Data, document);
        AppendResponses(output, operation.Data, document);
    }

    private static void AppendParameters(StringBuilder output, JsonElement pathItem, JsonElement operation, JsonElement document)
    {
        var parameters = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        AddParameters(pathItem, parameters, document);
        AddParameters(operation, parameters, document);
        if (parameters.Count == 0) return;

        output.AppendLine("#### Parameters");
        output.AppendLine("| Name | In | Required | Type | Description |");
        output.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var parameter in parameters.Values)
        {
            var schema = GetObject(parameter, "schema");
            var name = GetString(parameter, "name") ?? string.Empty;
            var location = GetString(parameter, "in") ?? string.Empty;
            var required = GetBoolean(parameter, "required") ? "Yes" : "No";
            output.AppendLine($"| {EscapeTableCell(name)} | {EscapeTableCell(location)} | {required} | {EscapeTableCell(SchemaType(schema))} | {EscapeTableCell(GetString(parameter, "description"))} |");
        }
        output.AppendLine();
    }

    private static void AddParameters(JsonElement item, IDictionary<string, JsonElement> parameters, JsonElement document)
    {
        if (!TryGet(item, "parameters", out var values) || values.ValueKind != JsonValueKind.Array) return;
        foreach (var parameter in values.EnumerateArray())
        {
            var resolvedParameter = ResolveReference(parameter, document);
            var key = $"{GetString(resolvedParameter, "in")}:{GetString(resolvedParameter, "name")}";
            parameters[key] = resolvedParameter;
        }
    }

    private static void AppendRequestBody(StringBuilder output, JsonElement operation, JsonElement document)
    {
        if (!TryGet(operation, "requestBody", out var requestBody)) return;
        requestBody = ResolveReference(requestBody, document);
        output.AppendLine("#### Request body");
        if (GetBoolean(requestBody, "required")) output.AppendLine("Required.");
        AppendMarkdown(output, GetString(requestBody, "description"));
        AppendContent(output, GetObject(requestBody, "content"), document);
    }

    private static void AppendResponses(StringBuilder output, JsonElement operation, JsonElement document)
    {
        if (!TryGet(operation, "responses", out var responses) || responses.ValueKind != JsonValueKind.Object) return;
        output.AppendLine("#### Responses");
        foreach (var response in responses.EnumerateObject())
        {
            var responseObject = ResolveReference(response.Value, document);
            output.AppendLine($"##### {CodeSpan(response.Name)}");
            AppendMarkdown(output, GetString(responseObject, "description"));
            AppendContent(output, GetObject(responseObject, "content"), document);
        }
    }

    private static void AppendContent(StringBuilder output, JsonElement content, JsonElement document)
    {
        if (content.ValueKind != JsonValueKind.Object) return;
        foreach (var media in content.EnumerateObject())
        {
            output.AppendLine($"##### {CodeSpan(media.Name)}");
            if (TryGet(media.Value, "schema", out var schema))
                output.AppendLine($"Schema: {CodeSpan(SchemaType(schema))}");

            if (TryGet(media.Value, "example", out var example))
                AppendExample(output, example);
            else if (TryGet(media.Value, "examples", out var examples) && examples.ValueKind == JsonValueKind.Object)
            {
                var firstExample = examples.EnumerateObject().Select(item => ResolveReference(item.Value, document)).FirstOrDefault();
                if (firstExample.ValueKind != JsonValueKind.Undefined && TryGet(firstExample, "value", out example))
                    AppendExample(output, example);
            }
        }
    }

    private static void AppendExample(StringBuilder output, JsonElement example)
    {
        var json = example.GetRawText();
        var fenceLength = Math.Max(3, LongestBacktickRun(json) + 1);
        var fence = new string('`', fenceLength);
        output.AppendLine($"{fence}json");
        output.AppendLine(json);
        output.AppendLine(fence);
        output.AppendLine();
    }

    private static void AppendSchemas(StringBuilder output, JsonElement document)
    {
        var schemas = GetObject(GetObject(document, "components"), "schemas");
        if (schemas.ValueKind != JsonValueKind.Object || !schemas.EnumerateObject().Any()) return;

        output.AppendLine("## Schemas");
        foreach (var schema in schemas.EnumerateObject())
        {
            output.AppendLine($"### {CodeSpan(schema.Name)}");
            output.AppendLine($"Type: {CodeSpan(SchemaType(schema.Value))}");
            AppendMarkdown(output, GetString(schema.Value, "description"));

            if (!TryGet(schema.Value, "properties", out var properties) || properties.ValueKind != JsonValueKind.Object) continue;
            var requiredItems = GetArray(schema.Value, "required");
            var required = requiredItems is { } requiredArray
                ? requiredArray.EnumerateArray().Select(item => item.GetString())
                    .OfType<string>().ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            output.AppendLine("| Property | Type | Required | Description |");
            output.AppendLine("| --- | --- | --- | --- |");
            foreach (var property in properties.EnumerateObject())
            {
                var isRequired = required.Contains(property.Name) ? "Yes" : "No";
                output.AppendLine($"| {EscapeTableCell(property.Name)} | {EscapeTableCell(SchemaType(property.Value))} | {isRequired} | {EscapeTableCell(GetString(property.Value, "description"))} |");
            }
            output.AppendLine();
        }
    }

    private static string SchemaType(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return "any";
        if (GetString(schema, "$ref") is { } reference)
            return reference[(reference.LastIndexOf('/') + 1)..].Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

        var type = GetString(schema, "type");
        if (type is null && TryGet(schema, "type", out var types) && types.ValueKind == JsonValueKind.Array)
            type = string.Join(" or ", types.EnumerateArray().Select(item => item.GetString()).Where(value => value is not null));
        if (type is null && TryGet(schema, "oneOf", out var oneOf) && oneOf.ValueKind == JsonValueKind.Array)
            return string.Join(" or ", oneOf.EnumerateArray().Select(SchemaType));
        if (type is null && TryGet(schema, "anyOf", out var anyOf) && anyOf.ValueKind == JsonValueKind.Array)
            return string.Join(" or ", anyOf.EnumerateArray().Select(SchemaType));
        if (type is null && TryGet(schema, "allOf", out var allOf) && allOf.ValueKind == JsonValueKind.Array)
            return string.Join(" & ", allOf.EnumerateArray().Select(SchemaType));
        if (type is null) type = TryGet(schema, "properties", out _) ? "object" : "any";

        var format = GetString(schema, "format");
        var display = string.IsNullOrWhiteSpace(format) ? type : $"{type} ({format})";
        if (type == "array" && TryGet(schema, "items", out var items)) display = $"array of {SchemaType(items)}";
        if (TryGet(schema, "enum", out var values) && values.ValueKind == JsonValueKind.Array)
            display += $": {string.Join(", ", values.EnumerateArray().Select(item => item.ToString()))}";
        return display;
    }

    private static int TagOrder(IReadOnlyList<string> tags, string tag)
    {
        for (var index = 0; index < tags.Count; index++)
            if (tags[index].Equals(tag, StringComparison.Ordinal)) return index;
        return int.MaxValue;
    }

    private static string EscapeMarkdown(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("*", "\\*", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal);

    private static string EscapeTableCell(string? value) =>
        (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string CodeSpan(string value)
    {
        var fence = new string('`', Math.Max(1, LongestBacktickRun(value) + 1));
        return $"{fence}{value}{fence}";
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

    private static void AppendMarkdown(StringBuilder output, string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;
        output.AppendLine(markdown.Trim());
        output.AppendLine();
    }

    private static JsonElement GetObject(JsonElement item, string key) =>
        TryGet(item, key, out var value) && value.ValueKind == JsonValueKind.Object ? value : default;

    private static JsonElement ResolveReference(JsonElement item, JsonElement document)
    {
        var resolved = item;
        var references = new HashSet<string>(StringComparer.Ordinal);
        while (GetString(resolved, "$ref") is { } reference &&
               reference.StartsWith("#/", StringComparison.Ordinal) && references.Add(reference))
        {
            var target = document;
            foreach (var segment in reference[2..].Split('/'))
            {
                var name = Uri.UnescapeDataString(segment)
                    .Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal);
                if (!TryGet(target, name, out var next)) return resolved;
                target = next;
            }
            resolved = target;
        }
        return resolved;
    }

    private static JsonElement? GetArray(JsonElement item, string key) =>
        TryGet(item, key, out var value) && value.ValueKind == JsonValueKind.Array ? value : null;

    private static bool TryGet(JsonElement item, string key, out JsonElement value)
    {
        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out value)) return true;
        value = default;
        return false;
    }

    private static string? GetString(JsonElement item, string key) =>
        TryGet(item, key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool GetBoolean(JsonElement item, string key) =>
        TryGet(item, key, out var value) && value.ValueKind == JsonValueKind.True;

    private sealed record Operation(string Path, string Method, string Tag, JsonElement PathItem, JsonElement Data);
}
