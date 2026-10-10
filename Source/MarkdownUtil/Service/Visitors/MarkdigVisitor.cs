using Markdown.Renderer;
using MarkdownUtil.Model;

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
                _htmlFoundation.WriteHtml(sw, entity.Title, "", graphDepth, markdown, editPageLink).GetAwaiter().GetResult();

            File.Move(temporaryPath, outputFileName, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        return true;
    }
}
