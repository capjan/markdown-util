using System.Xml;
using Core.Collections.NodeGraph.Extensions;
using Markdown.Renderer;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace MarkdownUtil.Commands;

public sealed class PdfCommand(GraphBuilder graphBuilder, PdfRenderer pdfRenderer)
{
    public int Execute(RenderCommandSettings settings)
    {
        string rootPath;
        string? singleFilePath = null;
        try
        {
            if (settings.RootPath is { } configuredPath && File.Exists(Path.GetFullPath(configuredPath)))
            {
                singleFilePath = Path.GetFullPath(configuredPath);
                if (!Path.GetExtension(singleFilePath).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"PDF input must be a Markdown file: {singleFilePath}");
                rootPath = Path.GetDirectoryName(singleFilePath)!;
            }
            else
            {
                rootPath = CommandPathDefaults.ResolveRootPath(settings.RootPath);
            }
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        var defaultOutputName = singleFilePath is null ? "documentation.pdf" : $"{Path.GetFileNameWithoutExtension(singleFilePath)}.pdf";
        var outputPath = Path.GetFullPath(settings.OutputPath ?? Path.Combine(Environment.CurrentDirectory, defaultOutputName));
        var graph = graphBuilder.CreateGraph(new RenderCommandSettings
        {
            RootPath = rootPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden
        });
        var files = Flatten(graph);
        if (singleFilePath is not null)
            files = files.Where(item => Path.GetFullPath(item.File.FileInfo.FullName).Equals(singleFilePath, StringComparison.Ordinal));
        var sections = files.Where(item => IsPdfVisible(item.File.FrontMatter))
            .Select(item => new PdfSourceSection(
                GetTitle(item.File),
                GetRelativeHtmlPath(rootPath, item.File),
                RemoveFrontMatter(File.ReadAllText(item.File.FileInfo.FullName), item.File.FrontMatter),
                item.Depth))
            .ToArray();

        if (sections.Length == 0)
        {
            Console.Error.WriteLine("No Markdown pages selected for PDF output.");
            return 1;
        }

        var outputDirectory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(outputDirectory);
        var temporaryPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                pdfRenderer.WritePdf(stream, Path.GetFileNameWithoutExtension(outputPath), sections, includeFrontMatter: singleFilePath is null);
            File.Move(temporaryPath, outputPath, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            Console.Error.WriteLine($"Could not write PDF: {exception.Message}");
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            return 1;
        }

        Console.WriteLine($"Wrote {sections.Length} Markdown files to {outputPath}");
        return 0;
    }

    private static IEnumerable<(MarkdownFile File, int Depth)> Flatten(IEnumerable<MarkdownFile> files, int depth = 0)
    {
        foreach (var file in files.OrderBy(file => file.SidebarOrder.HasValue ? 0 : 1)
                     .ThenBy(file => file.SidebarOrder)
                     .ThenBy(GetTitle, StringComparer.OrdinalIgnoreCase))
        {
            yield return (file, depth);
            foreach (var child in Flatten(file.Children, depth + 1)) yield return child;
        }
    }

    private static string GetTitle(MarkdownFile file) =>
        !string.IsNullOrWhiteSpace(file.Title)
            ? file.Title.Trim()
            : file.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)
                ? file.FileInfo.Directory?.Name ?? "Documentation"
                : Path.GetFileNameWithoutExtension(file.FileInfo.Name);

    private static string GetRelativeHtmlPath(string rootPath, MarkdownFile file)
    {
        var relativePath = Path.ChangeExtension(Path.GetRelativePath(rootPath, file.FileInfo.FullName), ".html");
        if (Path.GetFileName(relativePath).Equals("README.html", StringComparison.OrdinalIgnoreCase))
            relativePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, "index.html");
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string RemoveFrontMatter(string markdown, string frontMatter) =>
        frontMatter.Length > 0 && markdown.StartsWith(frontMatter, StringComparison.Ordinal)
            ? markdown[frontMatter.Length..].TrimStart('\r', '\n')
            : markdown;

    private static bool IsPdfVisible(string frontMatter)
    {
        if (!frontMatter.Contains("pdf", StringComparison.Ordinal)) return true;
        var lines = frontMatter.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[0].Trim() == "---") lines.RemoveAt(0);
        if (lines.Count > 0 && lines[^1].Trim() == "---") lines.RemoveAt(lines.Count - 1);

        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(string.Join(Environment.NewLine, lines)));
            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode metadata)
                return true;

            YamlNode? visibleNode = null;
            if (metadata.Children.TryGetValue(new YamlScalarNode("pdf"), out var pdfNode) && pdfNode is YamlMappingNode pdf &&
                pdf.Children.TryGetValue(new YamlScalarNode("visible"), out var nestedVisibleNode))
                visibleNode = nestedVisibleNode;
            if (visibleNode is null && metadata.Children.TryGetValue(new YamlScalarNode("pdf.visible"), out var compactVisibleNode))
                visibleNode = compactVisibleNode;

            return visibleNode is not YamlScalarNode scalar || !bool.TryParse(scalar.Value, out var visible) || visible;
        }
        catch (YamlException)
        {
            return true;
        }
    }
}
