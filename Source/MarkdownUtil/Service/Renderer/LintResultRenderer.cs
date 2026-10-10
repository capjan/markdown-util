using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Renderer;

public class LintResultRenderer
{
    private readonly ILinterErrorReceiver _receiver;

    public LintResultRenderer(ILinterErrorReceiver receiver)
    {
        this._receiver = receiver;
    }

    public int Render(IReadOnlyCollection<MarkdownFile> orphanedPages)
    {
        var errorCount = _receiver.ErrorCount;
        if (errorCount == 1)
        {
            Console.WriteLine($"Linter detected {errorCount} error.");
        }
        else if (errorCount > 1)
        {
            Console.WriteLine($"Linter detected {errorCount} errors.");
        }
        else
        {
            Console.WriteLine("Party! 🥳 The Markdown linter does not detect any errors!");
        }

        if (orphanedPages.Count > 0)
        {
            Console.WriteLine($"Found {orphanedPages.Count} completely orphaned pages:");
            foreach (var page in orphanedPages)
                Console.WriteLine($"  {page.FileInfo.FullName}");
        }

        return errorCount > 0 ? 1 : 0;
    }
}
