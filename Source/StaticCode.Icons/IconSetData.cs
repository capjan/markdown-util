using System.IO.Compression;
using System.Text.Json;

namespace StaticCode.Icons;

/// <summary>
/// An icon set in the reduced Iconify JSON format written by <c>update-icon-sets.sh</c>.
/// </summary>
internal sealed class IconSetData
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public string LicenseUrl { get; init; } = string.Empty;
    public int Width { get; init; } = 16;
    public int Height { get; init; } = 16;
    public Dictionary<string, IconData> Icons { get; init; } = new();
    public Dictionary<string, string> Aliases { get; init; } = new();

    public static IconSetData Load(string resourceName)
    {
        using var resource = typeof(IconSetData).Assembly.GetManifestResourceStream($"StaticCode.Icons.Res.{resourceName}.json.gz")
            ?? throw new InvalidOperationException($"The icon set '{resourceName}' is missing.");
        using var json = new GZipStream(resource, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<IconSetData>(json, JsonOptions)
            ?? throw new InvalidOperationException($"The icon set '{resourceName}' is empty.");
    }
}

internal sealed class IconData
{
    public string Body { get; init; } = string.Empty;
    public int? Width { get; init; }
    public int? Height { get; init; }
    public bool Hidden { get; init; }
}
