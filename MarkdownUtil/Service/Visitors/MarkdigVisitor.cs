using System.Text;
using System.Text.RegularExpressions;
using MarkdownRenderer;
using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class MarkdigVisitor : IVisitor<MarkdownFile>
{
    public MarkdigVisitor(string rootPath, string outPath, HtmlRenderer htmlFoundation)
    {
        this._rootPath = rootPath;
        _outPath = outPath;
        _htmlFoundation = htmlFoundation;
    }

    public GraphTraversalAlgorithm Algorithm => GraphTraversalAlgorithm.Default;
    private readonly string _rootPath;
    private readonly string _outPath;
    private readonly HtmlRenderer _htmlFoundation;
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var fi = entity.FileInfo;
        var fullPath = fi.FullName;

        var directoryPath = fi.DirectoryName ?? throw new InvalidOperationException();

        if (!fullPath.StartsWith(_rootPath))
        {
            return false;
        }

        var relativePath = fullPath.Substring(_rootPath.Length);
        var relativeFolder = directoryPath.Substring(_rootPath.Length);
        relativePath = relativePath.TrimStart(new[] {'/'});
        relativeFolder = relativeFolder.TrimStart(new[] {'/'});
        var outPath = Path.Combine(_outPath, relativePath);
        outPath = Path.ChangeExtension(outPath, ".html");
        var outDirectory = Path.Combine(_outPath, relativeFolder);
        AnsiConsole.WriteLine($"{relativePath} => {outPath}");

        var markdown = File.ReadAllText(fi.FullName);
     
        Directory.CreateDirectory(outDirectory);
        if (File.Exists(outPath)) File.Delete(outPath);
        using var fs = new FileStream(outPath, FileMode.CreateNew, FileAccess.Write);
        using var sw = new StreamWriter(fs);
        _htmlFoundation.WriteHeadAndBody(sw, entity.Title, "", graphDepth, markdown).Wait();
        // Markdig.Markdown.ToHtml(replacedMarkdown, sw);
        // _htmlFoundation.WriteAfterBody(sw);
        return true;
    }


}