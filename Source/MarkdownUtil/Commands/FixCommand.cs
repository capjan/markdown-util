using Core.Collections.NodeGraph.Extensions;
using Markdown.Linter;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class FixCommand
{
    private readonly GraphBuilder _graphBuilder;
    private readonly LinterVisitor _linterVisitor;

    public FixCommand(GraphBuilder graphBuilder, LinterVisitor linterVisitor)
    {
        _graphBuilder = graphBuilder;
        _linterVisitor = linterVisitor;
    }

    public int Execute(VisitorSettings settings)
    {
        string rootPath;
        try
        {
            rootPath = CommandPathDefaults.ResolveRootPath(settings.RootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        var graph = _graphBuilder.CreateGraph(new VisitorSettings
        {
            RootPath = rootPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden
        });
        _linterVisitor.Prepare(graph);
        graph.Visit(_linterVisitor);

        var pathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var moves = _linterVisitor.ErrorReceiver.Diagnostics
            .Where(diagnostic => diagnostic.IsWarning &&
                                 diagnostic.Message.StartsWith($"{RuleCodes.ReadmeWithoutSubpages}:", StringComparison.Ordinal))
            .Select(diagnostic => CreateMove(Path.GetFullPath(diagnostic.FilePath)))
            .ToArray();

        if (moves.Length == 0)
        {
            Console.WriteLine("No fixable warnings found.");
            return 0;
        }

        var movedPaths = moves.ToDictionary(move => move.SourcePath, move => move.TargetPath, pathComparer);
        var duplicateTargets = moves
            .GroupBy(move => move.TargetPath, pathComparer)
            .FirstOrDefault(group => group.Count() > 1);
        var existingTarget = moves.FirstOrDefault(move =>
            File.Exists(move.TargetPath) && !pathComparer.Equals(move.SourcePath, move.TargetPath));
        if (duplicateTargets is not null || existingTarget is not null)
        {
            var target = duplicateTargets?.Key ?? existingTarget!.TargetPath;
            Console.Error.WriteLine($"Cannot fix MDU006 because the target already exists: {target}");
            return 1;
        }

        var enumerationOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System,
            RecurseSubdirectories = true
        };
        var markdownFiles = Directory.EnumerateFiles(rootPath, "*", enumerationOptions)
            .Where(IsMarkdownFile)
            .ToArray();
        var changedDocuments = new Dictionary<string, string>(pathComparer);
        var updatedLinkCount = 0;

        foreach (var markdownFile in markdownFiles)
        {
            var sourcePath = Path.GetFullPath(markdownFile);
            var newSourcePath = movedPaths.GetValueOrDefault(sourcePath, sourcePath);
            var content = File.ReadAllText(sourcePath);
            var updatedContent = RewriteLinks(content, sourcePath, newSourcePath, movedPaths, pathComparer, out var changedLinkCount);
            if (updatedContent != content)
            {
                changedDocuments[sourcePath] = updatedContent;
                updatedLinkCount += changedLinkCount;
            }
        }

        foreach (var move in moves)
            File.Move(move.SourcePath, move.TargetPath);

        foreach (var (sourcePath, content) in changedDocuments)
        {
            var targetPath = movedPaths.GetValueOrDefault(sourcePath, sourcePath);
            File.WriteAllText(targetPath, content);
        }

        foreach (var move in moves)
        {
            var sectionDirectory = Path.GetDirectoryName(move.SourcePath)!;
            if (Directory.Exists(sectionDirectory) && !Directory.EnumerateFileSystemEntries(sectionDirectory).Any())
                Directory.Delete(sectionDirectory);

            Console.WriteLine($"Fixed MDU006: {Path.GetRelativePath(rootPath, move.SourcePath)} -> {Path.GetRelativePath(rootPath, move.TargetPath)}");
        }

        Console.WriteLine($"Updated {updatedLinkCount} Markdown link{(updatedLinkCount == 1 ? "" : "s")}.");
        return 0;
    }

    private static PageMove CreateMove(string sourcePath)
    {
        var sectionDirectory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        var targetPath = Path.Combine(sectionDirectory.Parent!.FullName, $"{sectionDirectory.Name}.md");
        return new PageMove(sourcePath, targetPath);
    }

    private static bool IsMarkdownFile(string path) =>
        Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(path).Equals(".markdown", StringComparison.OrdinalIgnoreCase);

    private static string RewriteLinks(
        string content,
        string sourcePath,
        string newSourcePath,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer,
        out int changedLinkCount)
    {
        var pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();
        var document = global::Markdig.Markdown.Parse(content, pipeline);
        var edits = new Dictionary<(int Start, int Length), string>();

        foreach (var link in document.Descendants<LinkInline>())
            AddEdit(link.UrlSpan, link.Url);

        foreach (var definition in document.GetLinkReferenceDefinitions(true).OfType<LinkReferenceDefinition>())
            AddEdit(definition.UrlSpan, definition.Url);

        var rewritten = content;
        foreach (var edit in edits.OrderByDescending(edit => edit.Key.Start))
        {
            rewritten = rewritten.Remove(edit.Key.Start, edit.Key.Length)
                .Insert(edit.Key.Start, edit.Value);
        }

        changedLinkCount = edits.Count;
        return rewritten;

        void AddEdit(SourceSpan span, string? destination)
        {
            if (destination is null || span.Start < 0 || span.Length <= 0 || span.Start + span.Length > content.Length)
                return;

            var newDestination = GetUpdatedDestination(
                sourcePath,
                newSourcePath,
                destination,
                movedPaths,
                pathComparer);
            if (newDestination is null) return;

            var original = content.Substring(span.Start, span.Length);
            var replacement = original.StartsWith('<') && original.EndsWith('>')
                ? $"<{newDestination}>"
                : newDestination;
            edits[(span.Start, span.Length)] = replacement;
        }
    }

    private static string? GetUpdatedDestination(
        string sourcePath,
        string newSourcePath,
        string destination,
        IReadOnlyDictionary<string, string> movedPaths,
        StringComparer pathComparer)
    {
        if (string.IsNullOrWhiteSpace(destination) ||
            destination.StartsWith("//", StringComparison.Ordinal) ||
            System.Text.RegularExpressions.Regex.IsMatch(destination, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
            return null;

        var suffixStart = destination.IndexOfAny(['?', '#']);
        var relativePath = suffixStart < 0 ? destination : destination[..suffixStart];
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        string targetPath;
        try
        {
            relativePath = Uri.UnescapeDataString(relativePath);
            if (Path.IsPathRooted(relativePath)) return null;

            targetPath = Path.GetFullPath(relativePath, Path.GetDirectoryName(sourcePath)!);
            if (Directory.Exists(targetPath))
            {
                var readmePath = Path.Combine(targetPath, "README.md");
                if (!File.Exists(readmePath)) return null;
                targetPath = readmePath;
            }
            else if (!File.Exists(targetPath))
            {
                return null;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return null;
        }

        var targetMoved = movedPaths.TryGetValue(targetPath, out var movedTargetPath);
        var finalTargetPath = targetMoved ? movedTargetPath! : targetPath;
        if (!targetMoved && pathComparer.Equals(sourcePath, newSourcePath)) return null;

        var relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(newSourcePath)!, finalTargetPath);
        var encodedTarget = string.Join('/', relativeTarget
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Select(Uri.EscapeDataString));
        var suffix = suffixStart < 0 ? string.Empty : destination[suffixStart..];
        return encodedTarget + suffix;
    }

    private sealed record PageMove(string SourcePath, string TargetPath);
}
