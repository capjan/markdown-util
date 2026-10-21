using System.Text.Encodings.Web;
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
        _topNavigationItems = new Dictionary<MarkdownFile, MarkdownFile?>();
        foreach (var page in Flatten(_navigationRoots))
            _topNavigationItems.Add(page, FindTopNavigationItem(page));
        _siteMediaProviders = FindSiteMediaProviders();
        _lastModifiedDates = GitLastModified.Read(rootPath);
    }
    
    private readonly HtmlRenderer _htmlFoundation;
    private readonly string _editPageRoot;
    private readonly SearchIndexBuilder _searchIndexBuilder;
    private readonly string _rootPath;
    private readonly string _outPath;
    private readonly MarkdownFile[] _navigationRoots;
    private readonly Dictionary<MarkdownFile, MarkdownFile?> _topNavigationItems;
    private readonly IReadOnlyCollection<string> _siteMediaProviders;
    private readonly IReadOnlyDictionary<string, DateTimeOffset> _lastModifiedDates;

    public IReadOnlyCollection<string> SiteMediaProviders => _siteMediaProviders;

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    protected override bool ProcessFile(MarkdownFile entity, int _, FilePathInfo io)
    {
        Console.WriteLine($"{io.InputRelativeFilePathToRootDirectory} => {io.OutputRelativeFilePathToRootDirectory}");

        var markdown = RemoveFrontMatter(io.ReadAllText(), entity.FrontMatter);
        io.CreateOutputDirectory();    // ensures that the output directory exits

        var editPageLink = string.IsNullOrEmpty(_editPageRoot) ? string.Empty : GetEditPageLink(io.InputRelativeFilePathToRootDirectory);
        var nestingDeep = Path.GetDirectoryName(io.OutputRelativeFilePathToRootDirectory)?
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        
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
                    RemoveIconShortcodes(entity.Title),
                    entity.Description,
                    nestingDeep,
                    markdown,
                    editPageLink,
                    BuildNavigation(entity, outputFileName),
                    BuildBreadcrumbs(entity, outputFileName),
                    _siteMediaProviders,
                    entity.Icon,
                    BuildTopNavigation(entity, outputFileName),
                    GetLastModified(entity),
                    GetRelativeUrl(
                        GetSiteRelativeSegments(Path.GetDirectoryName(Path.GetFullPath(outputFileName))!),
                        ["documentation.pdf"])).GetAwaiter().GetResult();

            File.Move(temporaryPath, outputFileName, true);
            WriteAliasPage(entity, outputFileName);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        return true;
    }

    private void WriteAliasPage(MarkdownFile page, string outputFileName)
    {
        if (page.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)) return;
        var aliasDirectory = Path.Combine(page.FileInfo.DirectoryName!, Path.GetFileNameWithoutExtension(page.FileInfo.Name));
        if (Directory.Exists(aliasDirectory) && Directory.EnumerateFiles(aliasDirectory).Any(filePath =>
                Path.GetFileName(filePath).Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(filePath).Equals("index.md", StringComparison.OrdinalIgnoreCase))) return;

        var aliasPath = Path.Combine(Path.GetDirectoryName(outputFileName)!, Path.GetFileNameWithoutExtension(outputFileName), "index.html");
        Directory.CreateDirectory(Path.GetDirectoryName(aliasPath)!);
        var relativeTarget = "../" + Uri.EscapeDataString(Path.GetFileName(outputFileName));
        var encodedTarget = HtmlEncoder.Default.Encode(relativeTarget);
        File.WriteAllText(aliasPath,
            $"<!doctype html><html><head><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url={encodedTarget}\"><link rel=\"canonical\" href=\"{encodedTarget}\"><title>Moved</title></head><body><a href=\"{encodedTarget}\">Continue to the page</a></body></html>");
    }

    private DateTimeOffset? GetLastModified(MarkdownFile page)
    {
        var relativePath = Path.GetRelativePath(_rootPath, page.FileInfo.FullName).Replace(Path.DirectorySeparatorChar, '/');
        return _lastModifiedDates.TryGetValue(relativePath, out var date) ? date : null;
    }

    private IReadOnlyCollection<string> FindSiteMediaProviders()
    {
        var providers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in Flatten(_navigationRoots))
        {
            var markdown = RemoveFrontMatter(File.ReadAllText(page.FileInfo.FullName), page.FrontMatter);
            providers.UnionWith(_htmlFoundation.FindMediaProviders(markdown));
        }

        return new[] { "YouTube", "Vimeo" }.Where(providers.Contains).ToArray();
    }

    private static string RemoveFrontMatter(string markdown, string frontMatter)
    {
        if (!string.IsNullOrEmpty(frontMatter) && markdown.StartsWith(frontMatter, StringComparison.Ordinal))
            return markdown[frontMatter.Length..].TrimStart('\r', '\n');

        return markdown;
    }

    private static IEnumerable<MarkdownFile> Flatten(IEnumerable<MarkdownFile> pages)
    {
        foreach (var page in pages)
        {
            yield return page;
            foreach (var child in Flatten(page.Children))
                yield return child;
        }
    }

    private IReadOnlyList<NavigationItem> BuildNavigation(MarkdownFile page, string currentOutputPath)
    {
        var currentPath = Path.GetFullPath(currentOutputPath);
        var currentDirectorySegments = GetSiteRelativeSegments(Path.GetDirectoryName(currentPath)!);
        var entries = CreateNavigationEntries(_navigationRoots);
        var activeTopNavigationItem = _topNavigationItems.GetValueOrDefault(page);
        var activeTopNavigationDepth = activeTopNavigationItem is null
            ? -1
            : entries.FirstOrDefault(entry => ReferenceEquals(entry.Page, activeTopNavigationItem))?.Depth ?? -1;
        if (activeTopNavigationDepth < 0) activeTopNavigationItem = null;
        return entries
            .Select(entry => new NavigationItem
            {
                Title = entry.Title,
                Url = GetRelativeUrl(currentDirectorySegments, entry.UrlSegments),
                Depth = entry.Depth,
                IsCurrent = string.Equals(entry.OutputPath, currentPath, PathComparison),
                IsCurrentSection = ReferenceEquals(page, entry.Page) || IsDescendant(page, entry.Page),
                IsExpandedByDefault = entry.IsExpandedByDefault,
                IsTopNavigationItem = IsTopNavigationPage(entry.Page),
                IsInCurrentTopSection = activeTopNavigationItem is not null &&
                                        ReferenceEquals(_topNavigationItems[entry.Page], activeTopNavigationItem),
                IsAncestorOfCurrentTopSection = activeTopNavigationItem is not null &&
                                                IsDescendant(activeTopNavigationItem, entry.Page),
                TopNavigationDepth = activeTopNavigationDepth < 0
                    ? 0
                    : entry.Depth - activeTopNavigationDepth - 1
            })
            .ToArray();
    }

    private static IEnumerable<MarkdownFile> OrderPages(IEnumerable<MarkdownFile> pages) => pages
        .OrderBy(page => page.NavigationOrder.HasValue ? 0 : 1)
        .ThenBy(page => page.NavigationOrder)
        .ThenBy(GetNavigationTitle, StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<NavigationItem> BuildTopNavigation(MarkdownFile page, string currentOutputPath)
    {
        var currentPath = Path.GetFullPath(currentOutputPath);
        var currentDirectorySegments = GetSiteRelativeSegments(Path.GetDirectoryName(currentPath)!);
        var activeTab = _topNavigationItems.GetValueOrDefault(page);
        return Flatten(_navigationRoots)
            .Where(IsTopNavigationPage)
            .OrderBy(candidate => candidate.NavigationOrder.HasValue ? 0 : 1)
            .ThenBy(candidate => candidate.NavigationOrder)
            .ThenBy(GetNavigationTitle, StringComparer.OrdinalIgnoreCase)
            .Select(candidate =>
            {
                var candidatePath = GetOutputPagePath(FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath,
                    candidate.FileInfo, ".html").OutputFullFilePath);
                var isCurrentPage = string.Equals(candidatePath, currentPath, PathComparison);
                return new NavigationItem
                {
                    Title = GetNavigationTitle(candidate),
                    Url = GetRelativeUrl(currentDirectorySegments, GetSiteRelativeSegments(candidatePath)),
                    IsCurrent = isCurrentPage,
                    IsCurrentSection = ReferenceEquals(candidate, activeTab)
                };
            })
            .ToArray();
    }

    private IReadOnlyList<NavigationEntry> CreateNavigationEntries(IEnumerable<MarkdownFile> roots)
    {
        var entries = new List<NavigationEntry>();
        AddPages(roots, 0);
        return entries;

        void AddPages(IEnumerable<MarkdownFile> pages, int depth)
        {
            foreach (var page in OrderPages(pages))
            {
                if (page.NavigationVisible)
                {
                    var pagePath = GetOutputPagePath(FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, page.FileInfo, ".html")
                        .OutputFullFilePath);
                    entries.Add(new NavigationEntry(page, GetNavigationTitle(page), pagePath, GetSiteRelativeSegments(pagePath), depth,
                        page.NavigationExpanded == true));
                }

                AddPages(page.Children, page.NavigationVisible ? depth + 1 : depth);
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

    private sealed record NavigationEntry(MarkdownFile Page, string Title, string OutputPath, string[] UrlSegments, int Depth, bool IsExpandedByDefault);

    private static bool IsDescendant(MarkdownFile page, MarkdownFile ancestor)
    {
        for (var current = page.Parent; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor)) return true;
        }

        return false;
    }

    private MarkdownFile? FindTopNavigationItem(MarkdownFile page)
    {
        for (var current = page; current is not null; current = current.Parent)
        {
            if (IsTopNavigationPage(current)) return current;
        }

        return null;
    }

    private bool IsTopNavigationPage(MarkdownFile page) =>
        page.NavigationVisible &&
        page.NavigationPlacement == NavigationPlacement.Top &&
        IsTopLevelNavigationPage(page.FileInfo);

    private bool IsTopLevelNavigationPage(FileInfo file)
    {
        var relativePath = Path.GetRelativePath(_rootPath, file.FullName);
        if (Path.IsPathRooted(relativePath) || relativePath == ".." ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return false;

        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Length == 1) return true;
        return segments.Length == 2 &&
               (file.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                file.Name.Equals("index.md", StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<BreadcrumbItem> BuildBreadcrumbs(MarkdownFile page, string currentOutputPath)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var ancestors = new List<MarkdownFile>();
        for (var parent = page.Parent; parent is not null; parent = parent.Parent)
            ancestors.Add(parent);
        ancestors.Reverse();

        var currentDirectory = Path.GetDirectoryName(Path.GetFullPath(currentOutputPath))!;
        foreach (var ancestor in ancestors.Where(ancestor => ancestor.NavigationVisible))
        {
            var ancestorPath = FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, ancestor.FileInfo, ".html")
                .OutputFullFilePath;
            breadcrumbs.Add(new BreadcrumbItem(
                GetNavigationTitle(ancestor),
                GetRelativeUrl(currentDirectory, GetOutputPagePath(ancestorPath))));
        }

        if (page.NavigationVisible)
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
        if (!string.IsNullOrWhiteSpace(page.NavigationTitle)) return RemoveIconShortcodes(page.NavigationTitle).Trim();
        if (!string.IsNullOrWhiteSpace(page.Title)) return RemoveIconShortcodes(page.Title).Trim();

        return page.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)
            ? page.FileInfo.Directory?.Name ?? "Documentation"
            : Path.GetFileNameWithoutExtension(page.FileInfo.Name);
    }

    private string GetEditPageLink(string relativePath)
    {
        var segments = relativePath.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString);
        return $"{_editPageRoot.TrimEnd('/')}/{string.Join('/', segments)}";
    }

    private static string RemoveIconShortcodes(string title) =>
        System.Text.RegularExpressions.Regex.Replace(title,
            @"(?::(?:material|simple|octicons|fontawesome|dashboard)-[\w-]+:\s*)+", string.Empty);
}
