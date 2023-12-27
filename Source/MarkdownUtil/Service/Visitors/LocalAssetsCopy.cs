using System.Text.RegularExpressions;
using MarkdownUtil.Model;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class LocalAssetsCopy : BaseVisitor
{
 
    public LocalAssetsCopy(string rootPath, string outPath) : base(rootPath, outPath)
    {
    }

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        var fileContent = io.ReadAllText();

        var assetLinks = Regex.Matches(fileContent, @"(?<=\(\s*)(\.{0,2}/)?(\.{2}/){0,}([\w\.-]+/){0,}[\w\.-]+\.(bmp|png|gif|jpg|jpeg|svg|pdf|txt)(?=\s*\))");
        if (assetLinks.Count > 0)
        {
            AnsiConsole.WriteLine($"Found {assetLinks.Count} Assets in {io.InputFullFilePath}");
            foreach (Match match in assetLinks)
            {
                var link = match.Value;
                var fullPath = Path.GetFullPath(link, io.InputFullDirectoryPath);
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    AnsiConsole.WriteLine($"Missing File: {link}");
                    continue;
                }
                
                var assetIO = FilePathInfo.CreateRenderFileInfo(io.RootDirectoryPath, io.OutputRootDirectoryPath,
                    new FileInfo(fullPath));
                assetIO.CopyFile();
                AnsiConsole.WriteLine($"Copied: {assetIO.InputRelativeFilePathToRootDirectory}");
            }
        }
        return true;
    }
}