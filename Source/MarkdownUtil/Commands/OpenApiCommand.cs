using Markdown.Renderer;
using Markdown.Search;

namespace MarkdownUtil.Commands;

public sealed class OpenApiCommand(HtmlRenderer renderer, SearchIndexBuilder searchIndexBuilder)
{
    public int Execute(string specificationPath, string? outputPath)
    {
        string fullSpecificationPath;
        string outputDirectory;
        try
        {
            fullSpecificationPath = Path.GetFullPath(specificationPath);
            if (!File.Exists(fullSpecificationPath))
                throw new InvalidOperationException($"OpenAPI file not found: {fullSpecificationPath}");

            outputDirectory = Path.GetFullPath(outputPath ?? Path.Combine(Environment.CurrentDirectory, "openapi-site"));
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        try
        {
            var markdownPath = Path.Combine(Path.GetDirectoryName(fullSpecificationPath)!, "openapi.md");
            var markdown = OpenApiMarkdownRenderer.AppendFromFile(string.Empty, markdownPath, fullSpecificationPath);
            var title = Path.GetFileNameWithoutExtension(fullSpecificationPath);

            Directory.CreateDirectory(outputDirectory);
            renderer.PrepareBasePath(outputDirectory);
            SearchClientAsset.WriteTo(outputDirectory);
            searchIndexBuilder.Clear();
            searchIndexBuilder.AddPage(title, "index.html", markdown);
            searchIndexBuilder.WriteIndex(outputDirectory);

            using var writer = File.CreateText(Path.Combine(outputDirectory, "index.html"));
            renderer.WriteHtml(writer, title, string.Empty, 0, markdown, string.Empty).GetAwaiter().GetResult();
            Console.WriteLine($"Rendered OpenAPI document to {outputDirectory}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not render OpenAPI document: {exception.Message}");
            return 1;
        }
    }
}
