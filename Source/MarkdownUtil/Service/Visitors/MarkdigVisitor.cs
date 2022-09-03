using Markdown.Renderer;
using MarkdownUtil.Model;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class MarkdigVisitor : BaseVisitor
{
    public MarkdigVisitor(
        string rootPath, 
        string outPath, 
        HtmlRenderer htmlFoundation, 
        string editPageRoot) : base(rootPath, outPath, ".html")
    {
        _htmlFoundation = htmlFoundation;
        _editPageRoot = editPageRoot;
    }
    
    private readonly HtmlRenderer _htmlFoundation;
    private readonly string _editPageRoot;

    protected override bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo io)
    {
        AnsiConsole.WriteLine($"{io.InputRelativeFilePathToRootDirectory} => {io.OutputRelativeFilePathToRootDirectory}");

        var markdown = io.ReadAllText();
        io.CreateOutputDirectory();    // ensures that the output directory exits
        io.DeleteOutputFileIfExists(); // deletes old output file if it exits

        var editPageLink = string.IsNullOrEmpty(_editPageRoot) ? string.Empty : Path.Combine(_editPageRoot, io.InputRelativeFilePathToRootDirectory);
        
        var outputFileName = io.OutputFullFilePath;
   
        if (outputFileName.EndsWith("readme.html", StringComparison.InvariantCultureIgnoreCase))
            outputFileName = outputFileName.Substring(0, outputFileName.Length - 11) + "index.html";
        // Delete the Output file if it already exits
        if (File.Exists(outputFileName)) File.Delete(outputFileName);
        using var fs = new FileStream(outputFileName, FileMode.CreateNew, FileAccess.Write);
        using var sw = new StreamWriter(fs);
        _htmlFoundation.WriteHtml(sw, entity.Title, "", graphDepth, markdown, editPageLink).Wait();
        return true;
    }
}