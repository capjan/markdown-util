using Markdown.Renderer;
using MarkdownUtil.Model;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class MarkdigVisitor : BaseVisitor
{
    public MarkdigVisitor(string rootPath, string outPath, HtmlRenderer htmlFoundation) : base(rootPath, outPath, ".html")
    {
        _htmlFoundation = htmlFoundation;
    }
    
    private readonly HtmlRenderer _htmlFoundation;

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        AnsiConsole.WriteLine($"{io.InputRelativeFilePathToRootDirectory} => {io.OutputRelativeFilePathToRootDirectory}");

        var markdown = io.ReadAllText();
        io.CreateOutputDirectory();
        io.DeleteOutputFileIfExists();

        var outputFileName = io.OutputFullFilePath;
        if (outputFileName.EndsWith("readme.html", StringComparison.InvariantCultureIgnoreCase))
            outputFileName = outputFileName.Substring(0, outputFileName.Length - 11) + "index.html";
        using var fs = new FileStream(outputFileName, FileMode.CreateNew, FileAccess.Write);
        using var sw = new StreamWriter(fs);
        _htmlFoundation.WriteHtml(sw, entity.Title, "", graphDepth, markdown).Wait();
        return true;
    }
}