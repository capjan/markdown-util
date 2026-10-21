using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Renderer;

namespace MarkdownUtilTest;

public class LintFormatTest
{
    private static (int Exit, string Output) Render(LintFormat format, string rootPath, params LintDiagnostic[] diagnostics)
    {
        var receiver = new ConsoleLintErrorReceiver();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.IsWarning) receiver.AddWarning(diagnostic.FilePath, diagnostic.LineNumber, diagnostic.Message);
            else receiver.Add(diagnostic.FilePath, diagnostic.LineNumber, diagnostic.Message);
        }

        var output = new StringWriter();
        var exit = new LintResultRenderer(receiver).Render(format, Array.Empty<MarkdownFile>(), rootPath, output);
        return (exit, output.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void WritesOneJsonDocumentWithParsedRuleCodes()
    {
        var root = Path.Combine(Path.GetTempPath(), "docs");
        var (exit, output) = Render(LintFormat.Json, root,
            new LintDiagnostic(Path.Combine(root, "Guide", "README.md"), 7, "MDU004 LocalLinkExists: Local link target './intro.md' does not exist.", false),
            new LintDiagnostic(Path.Combine(root, "Guide"), 1, "MDU006: Only page in 'Guide'.", true));

        Assert.Equal(1, exit);
        using var json = System.Text.Json.JsonDocument.Parse(output);
        var diagnostics = json.RootElement.GetProperty("diagnostics");
        Assert.Equal(1, json.RootElement.GetProperty("errors").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("warnings").GetInt32());
        Assert.Equal("Guide/README.md", diagnostics[0].GetProperty("file").GetString());
        Assert.Equal(7, diagnostics[0].GetProperty("line").GetInt32());
        Assert.Equal("MDU004", diagnostics[0].GetProperty("code").GetString());
        Assert.Equal("LocalLinkExists", diagnostics[0].GetProperty("rule").GetString());
        Assert.Equal("Local link target './intro.md' does not exist.", diagnostics[0].GetProperty("message").GetString());
        Assert.Equal("warning", diagnostics[1].GetProperty("severity").GetString());
        Assert.Contains("'./intro.md'", output);
    }

    [Fact]
    public void WritesGitHubAnnotationsWithEscapedValues()
    {
        var file = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Guide, Part 1.md");
        var (exit, output) = Render(LintFormat.GitHub, Directory.GetCurrentDirectory(),
            new LintDiagnostic(file, 3, "MDU004 LocalLinkExists: 100% broken: a.md", false));

        Assert.Equal(1, exit);
        Assert.StartsWith("::error file=Docs/Guide%2C Part 1.md,line=3,title=MDU004 LocalLinkExists::100%25 broken: a.md\n", output);
        Assert.EndsWith("Linter detected 1 error and 0 warnings.\n", output);
    }

    [Fact]
    public void ReturnsSuccessWithoutErrors()
    {
        var (exit, output) = Render(LintFormat.Json, Path.GetTempPath());

        Assert.Equal(0, exit);
        Assert.Contains("\"errors\": 0", output);
    }
}
