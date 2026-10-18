using System.Text.RegularExpressions;
using Markdown.Document;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class LocalAssetsCopy : BaseVisitor
{
    private static readonly Regex SchemePattern = new(@"^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.Compiled);

    public LocalAssetsCopy(string rootPath, string outPath) : base(rootPath, outPath)
    {
    }

    public static IEnumerable<string> GetLinkedAssetPaths(string markdown, string inputDirectoryPath) =>
        MarkdownDocument.ReadLinks(markdown)
            .Select(link => GetLocalAssetPath(link.Destination, inputDirectoryPath))
            .Where(path => path is not null)
            .Select(path => path!)
            .Distinct(StringComparer.Ordinal);

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        var fileContent = io.ReadAllText();

        var assetPaths = GetLinkedAssetPaths(fileContent, io.InputFullDirectoryPath).ToArray();
        if (assetPaths.Length > 0)
        {
            Console.WriteLine($"Found {assetPaths.Length} Assets in {io.InputFullFilePath}");
            foreach (var fullPath in assetPaths)
            {
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"Missing File: {fullPath}");
                    continue;
                }
                
                var assetIO = FilePathInfo.CreateRenderFileInfo(io.RootDirectoryPath, io.OutputRootDirectoryPath,
                    new FileInfo(fullPath));
                assetIO.CopyFile();
                Console.WriteLine($"Copied: {assetIO.InputRelativeFilePathToRootDirectory}");
            }
        }
        return true;
    }

    private static string? GetLocalAssetPath(string destination, string inputDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(destination) || destination.StartsWith('#') ||
            destination.StartsWith("//", StringComparison.Ordinal) || SchemePattern.IsMatch(destination))
            return null;

        var suffixStart = destination.IndexOfAny(['?', '#']);
        var relativePath = suffixStart < 0 ? destination : destination[..suffixStart];
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath) ||
                Path.GetExtension(relativePath).Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(relativePath).Equals(".markdown", StringComparison.OrdinalIgnoreCase))
                return null;

            return Path.GetFullPath(relativePath, inputDirectoryPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return null;
        }
    }
}
