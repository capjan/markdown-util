using Markdown.Renderer;
using Markdown.Renderer.Res.Model;
using Markdown.Search;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class MarkdigVisitor : BaseVisitor
{
    public MarkdigVisitor(
        string rootPath, 
        string outPath, 
        HtmlRenderer htmlFoundation, 
        string editPageRoot,
        SearchIndexBuilder searchIndexBuilder,
        MarkdownFile[]? navigationRoots = null) : base(rootPath, outPath, ".html")
    {
        _rootPath = rootPath;
        _outPath = outPath;
        _htmlFoundation = htmlFoundation;
        _editPageRoot = editPageRoot;
        _searchIndexBuilder = searchIndexBuilder;
        _navigationRoots = navigationRoots ?? Array.Empty<MarkdownFile>();
    }
    
    private readonly HtmlRenderer _htmlFoundation;
    private readonly string _editPageRoot;
    private readonly SearchIndexBuilder _searchIndexBuilder;
    private readonly string _rootPath;
    private readonly string _outPath;
    private readonly MarkdownFile[] _navigationRoots;
    private IReadOnlyList<NavigationEntry>? _navigationEntries;

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        Console.WriteLine($"{io.InputRelativeFilePathToRootDirectory} => {io.OutputRelativeFilePathToRootDirectory}");

        var markdown = io.ReadAllText();
        if (!string.IsNullOrEmpty(entity.FrontMatter) &&
            markdown.StartsWith(entity.FrontMatter, StringComparison.Ordinal))
            markdown = markdown[entity.FrontMatter.Length..].TrimStart('\r', '\n');
        io.CreateOutputDirectory();    // ensures that the output directory exits

        var editPageLink = string.IsNullOrEmpty(_editPageRoot) ? string.Empty : Path.Combine(_editPageRoot, io.InputRelativeFilePathToRootDirectory);
        
        var outputFileName = io.OutputFullFilePath;
   
        if (outputFileName.EndsWith("readme.html", StringComparison.InvariantCultureIgnoreCase))
        {
            if (File.Exists(outputFileName)) File.Delete(outputFileName);
            outputFileName = outputFileName.Substring(0, outputFileName.Length - 11) + "index.html";
        }

        _searchIndexBuilder.AddPage(
            GetNavigationTitle(entity),
            GetSiteRelativeUrl(outputFileName),
            markdown);

        var temporaryPath = outputFileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var fs = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
                _htmlFoundation.WriteHtml(
                    sw,
                    entity.Title,
                    "",
                    graphDepth,
                    markdown,
                    editPageLink,
                    BuildNavigation(outputFileName),
                    BuildBreadcrumbs(entity, outputFileName)).GetAwaiter().GetResult();

            File.Move(temporaryPath, outputFileName, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        return true;
    }

    private IReadOnlyList<NavigationItem> BuildNavigation(string currentOutputPath)
    {
        var currentPath = Path.GetFullPath(currentOutputPath);
        var currentDirectorySegments = GetSiteRelativeSegments(Path.GetDirectoryName(currentPath)!);

        return NavigationEntries
            .Select(entry => new NavigationItem
            {
                Title = entry.Title,
                Url = GetRelativeUrl(currentDirectorySegments, entry.UrlSegments),
                Depth = entry.Depth,
                IsCurrent = string.Equals(entry.OutputPath, currentPath, PathComparison)
            })
            .ToArray();
    }

    // The sidebar lists every page, so its entries are collected once per visitor instead of once per page.
    private IReadOnlyList<NavigationEntry> NavigationEntries => _navigationEntries ??= CreateNavigationEntries();

    private IReadOnlyList<NavigationEntry> CreateNavigationEntries()
    {
        var entries = new List<NavigationEntry>();
        AddPages(_navigationRoots, 0);
        return entries;

        void AddPages(IEnumerable<MarkdownFile> pages, int depth)
        {
            foreach (var page in pages
                         .OrderBy(page => page.SidebarOrder.HasValue ? 0 : 1)
                         .ThenBy(page => page.SidebarOrder)
                         .ThenBy(GetNavigationTitle, StringComparer.OrdinalIgnoreCase))
            {
                if (page.SidebarVisible)
                {
                    var pagePath = GetOutputPagePath(FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, page.FileInfo, ".html")
                        .OutputFullFilePath);
                    entries.Add(new NavigationEntry(GetNavigationTitle(page), pagePath, GetSiteRelativeSegments(pagePath), depth));
                }

                AddPages(page.Children, page.SidebarVisible ? depth + 1 : depth);
            }
        }
    }

    private string[] GetSiteRelativeSegments(string path)
    {
        var relativePath = Path.GetRelativePath(_outPath, path);
        return relativePath == "." ? Array.Empty<string>() : relativePath.Split(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Returns the URL of a page relative to a directory, both given as path segments relative to the site root;
    /// the same result as <see cref="Path.GetRelativePath"/> with escaped segments, without touching the file system.
    /// </summary>
    private static string GetRelativeUrl(string[] fromDirectory, string[] toFile)
    {
        var common = 0;
        while (common < fromDirectory.Length && common < toFile.Length - 1 &&
               string.Equals(fromDirectory[common], toFile[common], PathComparison))
            common++;

        return string.Join('/', Enumerable.Repeat("..", fromDirectory.Length - common)
            .Concat(toFile.Skip(common).Select(Uri.EscapeDataString)));
    }

    private sealed record NavigationEntry(string Title, string OutputPath, string[] UrlSegments, int Depth);

    private IReadOnlyList<BreadcrumbItem> BuildBreadcrumbs(MarkdownFile page, string currentOutputPath)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var ancestors = new Stack<MarkdownFile>();
        for (var parent = page.Parent; parent is not null; parent = parent.Parent)
            ancestors.Push(parent);

        var currentDirectory = Path.GetDirectoryName(Path.GetFullPath(currentOutputPath))!;
        while (ancestors.TryPop(out var ancestor))
        {
            var ancestorPath = FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, ancestor.FileInfo, ".html")
                .OutputFullFilePath;
            breadcrumbs.Add(new BreadcrumbItem(
                GetNavigationTitle(ancestor),
                GetRelativeUrl(currentDirectory, GetOutputPagePath(ancestorPath))));
        }

        breadcrumbs.Add(new BreadcrumbItem(GetNavigationTitle(page), null));
        return breadcrumbs;
    }

    private static string GetOutputPagePath(string pagePath)
    {
        if (pagePath.EndsWith("readme.html", StringComparison.OrdinalIgnoreCase))
            pagePath = pagePath[..^"readme.html".Length] + "index.html";

        return Path.GetFullPath(pagePath);
    }

    private static string GetRelativeUrl(string currentDirectory, string pagePath)
    {
        var relativeUrl = Path.GetRelativePath(currentDirectory, pagePath)
            .Replace(Path.DirectorySeparatorChar, '/');
        return string.Join('/', relativeUrl.Split('/').Select(Uri.EscapeDataString));
    }

    private string GetSiteRelativeUrl(string pagePath)
    {
        var relativePath = Path.GetRelativePath(_outPath, pagePath)
            .Replace(Path.DirectorySeparatorChar, '/');
        return string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));
    }

    private static string GetNavigationTitle(MarkdownFile page)
    {
        if (!string.IsNullOrWhiteSpace(page.Title)) return page.Title.Trim();

        return page.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)
            ? page.FileInfo.Directory?.Name ?? "Documentation"
            : Path.GetFileNameWithoutExtension(page.FileInfo.Name);
    }
}
