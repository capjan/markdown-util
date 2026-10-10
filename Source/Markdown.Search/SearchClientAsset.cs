namespace Markdown.Search;

public static class SearchClientAsset
{
    public static void WriteTo(string outputPath)
    {
        var assetDirectory = Path.Combine(outputPath, "js");
        Directory.CreateDirectory(assetDirectory);

        using var asset = typeof(SearchClientAsset).Assembly
            .GetManifestResourceStream("Markdown.Search.Res.search.js")
            ?? throw new InvalidOperationException("The search client asset is missing.");
        using var destination = File.Create(Path.Combine(assetDirectory, "search.js"));
        asset.CopyTo(destination);
    }
}
