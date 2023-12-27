using Core.Collections.NodeGraph;
using Markdown.Document;
using Markdown.Linter.Rules.Rule2;
using Markdown.Linter.Rules.Rule3;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class LinterVisitor: IVisitor<MarkdownFile>
{
    public readonly ILinterErrorReceiver ErrorReceiver;
    private readonly Rule2Validator _rule2Validator = new();
    private readonly Rule3Validator _rule3Validator = new();

    public LinterVisitor(ILinterErrorReceiver errorReceiver)
    {
        ErrorReceiver = errorReceiver;
    }

    public TraversalAlgorithm Algorithm { get; }
    
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var markdownFilePath = entity.FileInfo.FullName;
        var markdownContent = File.ReadAllText(entity.FileInfo.FullName);
        var title = entity.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorReceiver.Add(entity.FileInfo.FullName, 1, "Page must start with a headline 1 that defines its title. Add a '# pagetitle' to the first line of the document");
        }

        // Rule 2: The title of a markdown file must match the folder name 
        if (graphDepth > 0 && entity.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase))
        {
            var directoryName = entity.FileInfo.Directory?.Name ?? "";
            var rule2Input = new Rule2Input(title, directoryName);
            var result = _rule2Validator.Validate(rule2Input);
            ProcessValidationResult(markdownFilePath, 1, result);
        }
        
        // Rule 3: casing of links must match the casing on the current machine
        var linksOfFile = MarkdownDocument.ReadLinks(markdownContent);
        foreach (var link in linksOfFile)
        {
            var rule3Input = new Rule3Input(entity.FileInfo.FullName, link.Destination);
            var result = _rule3Validator.Validate(rule3Input);
            ProcessValidationResult(markdownFilePath, link.LineNumber, result);
        }
        
        return true;
    }

    private void ProcessValidationResult(
        string markdownFilePath,
        int lineNumber,
        FluentValidation.Results.ValidationResult result)
    {
        if (!result.IsValid)
        {
            ErrorReceiver.Add(markdownFilePath, lineNumber, result.ToString());
        }
    }
}
