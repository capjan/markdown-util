using System.Text.RegularExpressions;
using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Service.Visitors;

public class LinterVisitor: IVisitor<MarkdownFile>
{
    public readonly ILinterErrorReceiver ErrorReceiver;

    public LinterVisitor(ILinterErrorReceiver errorReceiver)
    {
        ErrorReceiver = errorReceiver;
    }

    public GraphTraversalAlgorithm Algorithm { get; }
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var title = entity.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorReceiver.Add(entity.FileInfo.FullName, 1, "Page must start with a headline 1 that defines its title. Add a '# pagetitle' to the first line of the document");
        }

        // Rule: Of subpages that are index the title must match the folder name 
        if (graphDepth > 0 && entity.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase))
        {
            var directoryName = entity.FileInfo.Directory?.Name ?? "";
            var shortTitle = Regex.Replace(title, "[^A-Za-z0-9-]", "").ToLower();
            if (!directoryName.Equals(shortTitle, StringComparison.OrdinalIgnoreCase))
            {
                ErrorReceiver.Add(entity.FileInfo.FullName, 1, $"The folder name of a index page must match its shortTitle. expected: {shortTitle}, found: {directoryName}");
            }
        }
            

        return true;
    }
}
