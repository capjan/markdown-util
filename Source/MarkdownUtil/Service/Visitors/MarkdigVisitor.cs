using Markdown.Renderer;
using Markdown.Renderer.Res.Model;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class MarkdigVisitor : BaseVisitor
{
    public MarkdigVisitor(
        string rootPath, 
        string outPath, 
        HtmlRenderer htmlFoundation, 
        string editPageRoot,
        MarkdownFile[]? navigationRoots = null) : base(rootPath, outPath, ".html")
    {
        _rootPath = rootPath;
        _outPath = outPath;
        _htmlFoundation = htmlFoundation;
        _editPageRoot = editPageRoot;
        _navigationRoots = navigationRoots ?? Array.Empty<MarkdownFile>();
    }
    
    private readonly HtmlRenderer _htmlFoundation;
    private readonly string _editPageRoot;
    private readonly string _rootPath;
    private readonly string _outPath;
    private readonly MarkdownFile[] _navigationRoots;

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        Console.WriteLine($"{io.InputRelativeFilePathToRootDirectory} => {io.OutputRelativeFilePathToRootDirectory}");

        var markdown = io.ReadAllText();
        io.CreateOutputDirectory();    // ensures that the output directory exits

        var editPageLink = string.IsNullOrEmpty(_editPageRoot) ? string.Empty : Path.Combine(_editPageRoot, io.InputRelativeFilePathToRootDirectory);
        
        var outputFileName = io.OutputFullFilePath;
   
        if (outputFileName.EndsWith("readme.html", StringComparison.InvariantCultureIgnoreCase))
        {
            if (File.Exists(outputFileName)) File.Delete(outputFileName);
            outputFileName = outputFileName.Substring(0, outputFileName.Length - 11) + "index.html";
        }

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
                    BuildNavigation(outputFileName)).GetAwaiter().GetResult();

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
            foreach (var page in pages.OrderBy(GetNavigationTitle, StringComparer.OrdinalIgnoreCase))
            {
                var pagePath = FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, page.FileInfo, ".html")
                    .OutputFullFilePath;
                if (pagePath.EndsWith("readme.html", StringComparison.OrdinalIgnoreCase))
                    pagePath = pagePath[..^"readme.html".Length] + "index.html";

                pagePath = Path.GetFullPath(pagePath);
                var relativeUrl = Path.GetRelativePath(currentDirectory, pagePath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var url = string.Join('/', relativeUrl.Split('/').Select(Uri.EscapeDataString));

                navigation.Add(new NavigationItem
                {
                    Title = GetNavigationTitle(page),
                    Url = url,
                    Depth = depth,
                    IsCurrent = string.Equals(pagePath, currentPath, pathComparison)
                });

                AddPages(page.Children, depth + 1);
            }
        }
    }

    private static string GetNavigationTitle(MarkdownFile page)
    {
        if (!string.IsNullOrWhiteSpace(page.Title)) return page.Title.Trim();

        return page.FileInfo.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase)
            ? page.FileInfo.Directory?.Name ?? "Dokumentation"
            : Path.GetFileNameWithoutExtension(page.FileInfo.Name);
    }
}
