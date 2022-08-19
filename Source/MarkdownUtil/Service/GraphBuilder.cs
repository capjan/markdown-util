using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service.Visitors;
using MarkdownUtil.Utils;

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

    public MarkdownGraph CreateGraph(IVisitorSettings settings)
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

    private MarkdownGraph CreateGraph(DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions searchOptions)
    {
        var list = GetMarkdownFilesAt(null, directoryInfo, searchPattern, searchOptions).ToList();
        return new MarkdownGraph(list);
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
        
        return markdownFilesInFolder
            .Select(file =>
            {
                if (file == usedParent)
                {
                    file.Children = file.FileInfo.Directory?
                                        .GetDirectories()
                                        .SelectMany(dirInfoOfChildren =>
                                            GetMarkdownFilesAt(usedParent, dirInfoOfChildren, searchPattern, searchOptions)).ToArray()
                                    ?? Array.Empty<MarkdownFile>();
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