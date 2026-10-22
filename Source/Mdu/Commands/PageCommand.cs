using Mdu.Service;

namespace Mdu.Commands;

/// <summary>Moves pages with their links and shows which pages link to a file.</summary>
public sealed class PageCommand
{
    public int Move(string? rootPath, string source, string target, bool includeHidden, bool dryRun, TextWriter? output = null)
    {
        output ??= Console.Out;
        if (ResolveRoot(rootPath) is not { } root) return 1;

        PageMovePlan plan;
        try
        {
            plan = PageMover.Plan(root, source, target, includeHidden);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        if (!dryRun) PageMover.Apply(plan);

        var moved = plan.IsDirectory ? $"folder with {plan.MovedFileCount} file{(plan.MovedFileCount == 1 ? "" : "s")}" : "page";
        output.WriteLine($"{(dryRun ? "Would move" : "Moved")} {moved}: {Relative(plan.RootPath, plan.SourcePath)} -> {Relative(plan.RootPath, plan.TargetPath)}");
        var linkCount = plan.ChangedPages.Sum(page => page.LinkCount);
        if (linkCount == 0)
        {
            output.WriteLine("No links needed to change.");
            return 0;
        }

        output.WriteLine($"{(dryRun ? "Would update" : "Updated")} {linkCount} link{(linkCount == 1 ? "" : "s")} in {plan.ChangedPages.Count} page{(plan.ChangedPages.Count == 1 ? "" : "s")}:");
        foreach (var page in plan.ChangedPages)
            output.WriteLine($"  {Relative(plan.RootPath, page.NewPath)} ({page.LinkCount})");
        if (!dryRun) output.WriteLine("Run 'mdu lint' to check the new page structure.");
        return 0;
    }

    public int Links(string? rootPath, string file, bool includeHidden, TextWriter? output = null)
    {
        output ??= Console.Out;
        if (ResolveRoot(rootPath) is not { } root) return 1;

        var targetPath = Path.GetFullPath(file);
        if (Directory.Exists(targetPath)) targetPath = Path.Combine(targetPath, "README.md");
        if (!File.Exists(targetPath))
        {
            Console.Error.WriteLine($"{file} does not exist.");
            return 1;
        }

        var pages = MarkdownLinks.EnumerateMarkdownFiles(root, includeHidden)
            .OrderBy(page => Relative(root, page), StringComparer.OrdinalIgnoreCase)
            .Select(page => (Page: page, Links: MarkdownLinks.Find(File.ReadAllText(page), page)
                .Where(link => MarkdownLinks.PathComparer.Equals(link.TargetPath, targetPath))
                .ToList()))
            .Where(page => page.Links.Count > 0)
            .ToList();

        foreach (var (page, links) in pages)
        foreach (var link in links)
            output.WriteLine($"{Relative(root, page)}:{link.Line}: {link.Markdown}");

        var linkCount = pages.Sum(page => page.Links.Count);
        output.WriteLine(linkCount == 0
            ? $"No page links to {Relative(root, targetPath)}."
            : $"{linkCount} link{(linkCount == 1 ? "" : "s")} in {pages.Count} page{(pages.Count == 1 ? "" : "s")} point{(linkCount == 1 ? "s" : "")} to {Relative(root, targetPath)}.");
        return 0;
    }

    private static string? ResolveRoot(string? rootPath)
    {
        try
        {
            return CommandPathDefaults.ResolveRootPath(rootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return null;
        }
    }

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
}
