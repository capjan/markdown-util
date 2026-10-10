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
        var navigation = new List<NavigationItem>();
        var currentPath = Path.GetFullPath(currentOutputPath);
        var currentDirectory = Path.GetDirectoryName(currentPath)!;
        var pathComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        AddPages(_navigationRoots, 0);
        return navigation;

        void AddPages(IEnumerable<MarkdownFile> pages, int depth)
        {
            foreach (var page in pages
                         .OrderBy(page => page.SidebarOrder.HasValue ? 0 : 1)
                         .ThenBy(page => page.SidebarOrder)
                         .ThenBy(GetNavigationTitle, StringComparer.OrdinalIgnoreCase))
            {
                var pagePath = FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, page.FileInfo, ".html")
                    .OutputFullFilePath;
                pagePath = GetOutputPagePath(pagePath);
                var url = GetRelativeUrl(currentDirectory, pagePath);

                if (page.SidebarVisible)
                {
                    navigation.Add(new NavigationItem
                    {
                        Title = GetNavigationTitle(page),
                        Url = url,
                        Depth = depth,
                        IsCurrent = string.Equals(pagePath, currentPath, pathComparison)
                    });
                }

                AddPages(page.Children, page.SidebarVisible ? depth + 1 : depth);
            }
        }
    }

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
