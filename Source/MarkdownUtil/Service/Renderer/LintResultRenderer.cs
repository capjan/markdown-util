using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Renderer;

public class LintResultRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Red = "\u001b[31m";
    private const string Yellow = "\u001b[33m";
    private const string Cyan = "\u001b[36m";
    private readonly ILinterErrorReceiver _receiver;

    public LintResultRenderer(ILinterErrorReceiver receiver)
    {
        this._receiver = receiver;
    }

    public int Render(
        IReadOnlyCollection<MarkdownFile> orphanedPages,
        string rootPath,
        bool pretty = false,
        bool color = false,
        TextWriter? output = null)
    {
        output ??= Console.Out;
        var errorCount = _receiver.ErrorCount;
        var warningCount = _receiver.WarningCount;

        if (errorCount == 0 && warningCount == 0)
        {
            output.WriteLine("Party! 🥳 The Markdown linter does not detect any errors!");
        }
        else
        {
            if (pretty)
                RenderPretty(_receiver.Diagnostics, rootPath, color, output);
            else
                RenderStandard(_receiver.Diagnostics, color, output);

            var summary = new List<string>();
            if (errorCount > 0) summary.Add($"{errorCount} {Style($"error{(errorCount == 1 ? "" : "s")}", Red, color)}");
            if (warningCount > 0) summary.Add($"{warningCount} {Style($"warning{(warningCount == 1 ? "" : "s")}", Yellow, color)}");
            output.WriteLine($"Linter detected {string.Join(" and ", summary)}.");
        }

        if (orphanedPages.Count > 0)
        {
            output.WriteLine($"Found {orphanedPages.Count} completely orphaned pages:");
            foreach (var page in orphanedPages)
            {
                var path = pretty ? Path.GetRelativePath(rootPath, page.FileInfo.FullName) : page.FileInfo.FullName;
                output.WriteLine($"  {path}");
            }
        }

        return errorCount > 0 ? 1 : 0;
    }

    private static void RenderStandard(IReadOnlyList<LintDiagnostic> diagnostics, bool color, TextWriter output)
    {
        foreach (var diagnostic in diagnostics)
        {
            var message = diagnostic.IsWarning ? $"warning {diagnostic.Message}" : diagnostic.Message;
            output.WriteLine($"{diagnostic.FilePath}:{diagnostic.LineNumber}: {Style(message, diagnostic.IsWarning ? Yellow : Red, color)}");
        }
    }

    private static void RenderPretty(
        IReadOnlyList<LintDiagnostic> diagnostics,
        string rootPath,
        bool color,
        TextWriter output)
    {
        foreach (var group in diagnostics
                     .GroupBy(diagnostic => diagnostic.FilePath)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            output.WriteLine(Style(Path.GetRelativePath(rootPath, group.Key), Cyan, color));
            foreach (var diagnostic in group.OrderBy(diagnostic => diagnostic.LineNumber))
            {
                var severity = diagnostic.IsWarning ? "warning" : "error";
                var severityColor = diagnostic.IsWarning ? Yellow : Red;
                output.WriteLine($"  {diagnostic.LineNumber}: {Style(severity, severityColor, color)} {diagnostic.Message}");
            }

            output.WriteLine();
        }
    }

    private static string Style(string value, string colorCode, bool color) =>
        color ? $"{colorCode}{value}{Reset}" : value;
}
