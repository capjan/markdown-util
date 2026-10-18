using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Service;

/// <summary>
/// Main Service to Process Markdown Files
/// </summary>
public class GraphBuilder
{

    private readonly DataPreparationVisitor _dataPreparationVisitor;

    public GraphBuilder(DataPreparationVisitor dataPreparationVisitor)
    {
        _dataPreparationVisitor = dataPreparationVisitor;
    }

    public MarkdownFile[] CreateGraph(IVisitorSettings settings)
    {
        var searchOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.System
                : FileAttributes.Hidden | FileAttributes.System,
            RecurseSubdirectories = false
        };

        var rootPath = settings.RootPath ?? Directory.GetCurrentDirectory();
        var searchPattern = settings.SearchPattern ?? Default.SearchPattern;
        
        var rootDirectory = new DirectoryInfo(rootPath);
        
        var graph = CreateGraph(rootDirectory, searchPattern, searchOptions);
        graph.Visit(_dataPreparationVisitor);
        return graph;
    }

    private MarkdownFile[] CreateGraph(DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions searchOptions)
    {
        return GetMarkdownFilesAt(null, directoryInfo, searchPattern, searchOptions).ToArray();
    }

    private static IEnumerable<MarkdownFile> GetMarkdownFilesAt(MarkdownFile? parent, DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions searchOptions)
    {
        var markdownFilesInFolder = directoryInfo
            .GetFiles(searchPattern, searchOptions)
            .Select(fileInfo => new MarkdownFile(fileInfo, parent))
            .ToList();
        var usedParent = markdownFilesInFolder
            .SingleOrDefault(itm => itm.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                                    itm.FileInfo.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase));

        if (usedParent == null)
        {
            foreach (var page in markdownFilesInFolder) page.Parent = parent;
            return markdownFilesInFolder
                .Concat(directoryInfo.GetDirectories().SelectMany(child => GetMarkdownFilesAt(parent, child, searchPattern, searchOptions)))
                .ToArray();
        }

        var siblingPages = markdownFilesInFolder
            .Where(file => file != usedParent)
            .ToArray();
        var directoryPages = directoryInfo
            .GetDirectories()
            .SelectMany(dirInfoOfChildren =>
                GetMarkdownFilesAt(usedParent, dirInfoOfChildren, searchPattern, searchOptions)).ToArray();
        usedParent.IsOnlyReadmeInSection = siblingPages.Length == 0 && directoryPages.Length == 0;
        usedParent.Children = siblingPages.Concat(directoryPages).ToArray();

        foreach (var page in siblingPages)
            page.Parent = usedParent;

        return [usedParent];
    }

}
