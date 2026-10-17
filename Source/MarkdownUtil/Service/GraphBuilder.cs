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
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System,
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
        if (markdownFilesInFolder.Count == 0) return markdownFilesInFolder;

        var usedParent = markdownFilesInFolder
            .SingleOrDefault(itm => itm.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase));

        if (usedParent == null) return markdownFilesInFolder;

        var childPages = directoryInfo
            .GetDirectories()
            .SelectMany(dirInfoOfChildren =>
                GetMarkdownFilesAt(usedParent, dirInfoOfChildren, searchPattern, searchOptions)).ToArray();
        usedParent.IsOnlyReadmeInSection = markdownFilesInFolder.Count == 1 && childPages.Length == 0;
        
        return markdownFilesInFolder
            .Select(file =>
            {
                if (file == usedParent)
                {
                    file.Children = childPages;
                }
                else
                {
                    file.Parent = usedParent;
                }
              
                return file;
            })
            .ToArray();
    }

}
