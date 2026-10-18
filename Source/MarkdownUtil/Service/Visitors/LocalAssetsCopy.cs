using System.Text.RegularExpressions;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class LocalAssetsCopy : BaseVisitor
{
    private static readonly Regex AssetLinkPattern = new(
        @"(?<=\(\s*)(\.{0,2}/)?(\.{2}/){0,}([\w\.-]+/){0,}[\w\.-]+\.(bmp|png|gif|jpg|jpeg|webp|svg|pdf|txt)(?=\s*\))",
        RegexOptions.Compiled);

    public LocalAssetsCopy(string rootPath, string outPath) : base(rootPath, outPath)
    {
    }

    public static IEnumerable<string> GetLinkedAssetPaths(string markdown, string inputDirectoryPath) =>
        AssetLinkPattern.Matches(markdown)
            .Select(match => Path.GetFullPath(match.Value, inputDirectoryPath))
            .Distinct(StringComparer.Ordinal);

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        var fileContent = io.ReadAllText();

        var assetLinks = AssetLinkPattern.Matches(fileContent);
        if (assetLinks.Count > 0)
        {
            Console.WriteLine($"Found {assetLinks.Count} Assets in {io.InputFullFilePath}");
            foreach (Match match in assetLinks)
            {
                var link = match.Value;
                var fullPath = Path.GetFullPath(link, io.InputFullDirectoryPath);
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"Missing File: {link}");
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
}
