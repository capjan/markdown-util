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
        var warningCount = _receiver.WarningCount;
        if (errorCount == 0 && warningCount == 0)
        {
            Console.WriteLine("Party! 🥳 The Markdown linter does not detect any errors!");
        }
        else
        {
            var summary = new List<string>();
            if (errorCount > 0) summary.Add($"{errorCount} error{(errorCount == 1 ? "" : "s")}");
            if (warningCount > 0) summary.Add($"{warningCount} warning{(warningCount == 1 ? "" : "s")}");
            Console.WriteLine($"Linter detected {string.Join(" and ", summary)}.");
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
