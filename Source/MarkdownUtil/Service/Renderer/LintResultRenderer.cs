using System.Text.Json;
using System.Text.RegularExpressions;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Renderer;

/// <summary>How <c>mdu lint</c> prints its results.</summary>
public enum LintFormat
{
    Text,

    /// <summary>One JSON document for scripts and tools.</summary>
    Json,

    /// <summary>GitHub Actions workflow commands, shown as annotations on the changed lines of a pull request.</summary>
    GitHub
}

public class LintResultRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Red = "\u001b[31m";
    private const string Yellow = "\u001b[33m";
    private const string Cyan = "\u001b[36m";
    private static readonly Regex DiagnosticMessage = new(@"^(?<code>MDU\d+)(?:\s+(?<rule>[A-Za-z]+))?:\s*(?<text>.*)$",
        RegexOptions.Compiled | RegexOptions.Singleline);
    // The output is read by tools, not embedded in HTML, so quotes and accents stay readable.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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

    /// <summary>Prints the results in a machine-readable format; returns 1 when there are errors.</summary>
    public int Render(
        LintFormat format,
        IReadOnlyCollection<MarkdownFile> orphanedPages,
        string rootPath,
        TextWriter? output = null)
    {
        output ??= Console.Out;
        switch (format)
        {
            case LintFormat.Json:
                RenderJson(orphanedPages, rootPath, output);
                break;
            case LintFormat.GitHub:
                RenderGitHub(orphanedPages, output);
                break;
            default:
                return Render(orphanedPages, rootPath, output: output);
        }

        return _receiver.ErrorCount > 0 ? 1 : 0;
    }

    private void RenderJson(IReadOnlyCollection<MarkdownFile> orphanedPages, string rootPath, TextWriter output)
    {
        var result = new
        {
            root = rootPath,
            errors = _receiver.ErrorCount,
            warnings = _receiver.WarningCount,
            diagnostics = _receiver.Diagnostics.Select(diagnostic =>
            {
                var (code, rule, text) = Parse(diagnostic.Message);
                return new
                {
                    file = Path.GetRelativePath(rootPath, diagnostic.FilePath).Replace(Path.DirectorySeparatorChar, '/'),
                    path = diagnostic.FilePath,
                    line = diagnostic.LineNumber,
                    severity = diagnostic.IsWarning ? "warning" : "error",
                    code,
                    rule,
                    message = text
                };
            }),
            orphanedPages = orphanedPages.Select(page =>
                Path.GetRelativePath(rootPath, page.FileInfo.FullName).Replace(Path.DirectorySeparatorChar, '/'))
        };
        output.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
    }

    // Paths are relative to the working directory, which is the repository root in a GitHub Actions job.
    private void RenderGitHub(IReadOnlyCollection<MarkdownFile> orphanedPages, TextWriter output)
    {
        foreach (var diagnostic in _receiver.Diagnostics)
        {
            var (code, rule, text) = Parse(diagnostic.Message);
            var title = rule is null ? code : $"{code} {rule}";
            output.WriteLine($"::{(diagnostic.IsWarning ? "warning" : "error")} " +
                             $"file={EscapeProperty(GitHubPath(diagnostic.FilePath))},line={diagnostic.LineNumber}," +
                             $"title={EscapeProperty(title ?? "mdu lint")}::{EscapeData(text)}");
        }

        foreach (var page in orphanedPages)
            output.WriteLine($"::warning file={EscapeProperty(GitHubPath(page.FileInfo.FullName))},line=1,title=Orphaned page::" +
                             EscapeData("This page is hidden from navigation and no visible page links to it."));

        // Orphaned pages appear as warning annotations, so the summary counts them as warnings too.
        var warningCount = _receiver.WarningCount + orphanedPages.Count;
        output.WriteLine($"Linter detected {_receiver.ErrorCount} error{(_receiver.ErrorCount == 1 ? "" : "s")} and " +
                         $"{warningCount} warning{(warningCount == 1 ? "" : "s")}.");
    }

    private static (string? Code, string? Rule, string Text) Parse(string message)
    {
        var match = DiagnosticMessage.Match(message);
        return match.Success
            ? (match.Groups["code"].Value, match.Groups["rule"].Success ? match.Groups["rule"].Value : null, match.Groups["text"].Value)
            : (null, null, message);
    }

    private static string GitHubPath(string path) =>
        Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace(Path.DirectorySeparatorChar, '/');

    private static string EscapeData(string value) =>
        value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");

    private static string EscapeProperty(string value) =>
        EscapeData(value).Replace(":", "%3A").Replace(",", "%2C");

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
