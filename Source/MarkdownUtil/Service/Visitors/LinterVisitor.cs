using Core.Collections.NodeGraph;
using Markdown.Document;
using Markdown.Linter;
using Markdown.Linter.Rules.Rule2;
using Markdown.Linter.Rules.Rule3;
using Markdown.Linter.Rules.Rule4;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class LinterVisitor: IVisitor<MarkdownFile>
{
    public readonly ILinterErrorReceiver ErrorReceiver;
    private readonly Rule2Validator _rule2Validator = new();
    private readonly Rule3Validator _rule3Validator = new();
    private readonly Rule4Validator _rule4Validator = new();
    private readonly List<MarkdownFile> _pages = new();
    private readonly HashSet<string> _linkedPages = new(PathComparer);

    private static StringComparer PathComparer => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public IReadOnlyList<MarkdownFile> OrphanedPages => _pages
        .Where(page => !page.SidebarVisible && !_linkedPages.Contains(Path.GetFullPath(page.FileInfo.FullName)))
        .OrderBy(page => page.FileInfo.FullName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public LinterVisitor(ILinterErrorReceiver errorReceiver)
    {
        ErrorReceiver = errorReceiver;
    }

    public TraversalAlgorithm Algorithm { get; }
    
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        _pages.Add(entity);
        var markdownFilePath = entity.FileInfo.FullName;
        var markdownContent = File.ReadAllText(entity.FileInfo.FullName);
        var title = entity.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorReceiver.Add(markdownFilePath, 1, $"{RuleCodes.HeadingRequired}: Page must start with a level-one heading that defines its title.");
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
            if (entity.SidebarVisible && !link.IsImageLink && TryResolvePageLink(entity, link.Destination, out var targetPath))
                _linkedPages.Add(targetPath);

            var rule3Input = new Rule3Input(entity.FileInfo.FullName, link.Destination);
            var casingResult = _rule3Validator.Validate(rule3Input);
            ProcessValidationResult(markdownFilePath, link.LineNumber, casingResult);

            // A casing mismatch already explains why the target cannot be resolved on this machine.
            if (!casingResult.IsValid) continue;

            var linkResult = _rule4Validator.Validate(new Rule4Input(entity.FileInfo.FullName, link.Destination));
            ProcessValidationResult(markdownFilePath, link.LineNumber, linkResult);
        }
        
        return true;
    }

    private static bool TryResolvePageLink(MarkdownFile source, string destination, out string targetPath)
    {
        targetPath = string.Empty;
        if (string.IsNullOrWhiteSpace(destination) || destination.StartsWith('#') ||
            destination.StartsWith("//", StringComparison.Ordinal) ||
            System.Text.RegularExpressions.Regex.IsMatch(destination, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
            return false;

        var suffixStart = destination.IndexOfAny(['?', '#']);
        var relativePath = suffixStart < 0 ? destination : destination[..suffixStart];
        if (string.IsNullOrWhiteSpace(relativePath)) return false;

        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath)) return false;

            var sourceDirectory = source.FileInfo.DirectoryName;
            if (string.IsNullOrEmpty(sourceDirectory)) return false;

            targetPath = Path.GetFullPath(relativePath, sourceDirectory);
            if (Directory.Exists(targetPath))
                targetPath = Path.Combine(targetPath, "README.md");

            return File.Exists(targetPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return false;
        }
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
