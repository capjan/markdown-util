using Core.Collections.NodeGraph;
using Markdown.Linter.Rules.Rule2;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class LinterVisitor: IVisitor<MarkdownFile>
{
    public readonly ILinterErrorReceiver ErrorReceiver;
    private readonly Rule2Validator _rule2Validator = new();

    public LinterVisitor(ILinterErrorReceiver errorReceiver)
    {
        ErrorReceiver = errorReceiver;
    }

    public TraversalAlgorithm Algorithm { get; }
    
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
            var rule2Input = new Rule2Input(title, directoryName);
            var result = _rule2Validator.Validate(rule2Input);
            if (!result.IsValid)
            {
                ErrorReceiver.Add(entity.FileInfo.FullName, 1, result.ToString());
            }
        }
        
        return true;
    }
}
