using Mdu.Model;
using Mdu.Service;
using Mdu.Service.Renderer;

namespace Mdu.Test;

public class LintResultRendererTest
{
    [Fact]
    public void PrettyOutputGroupsDiagnosticsByRelativeFilePathAndColorsSeverity()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "markdown-util-lint-test");
        var filePath = Path.Combine(rootPath, "Docs", "Publish", "README.md");
        var receiver = new ConsoleLintErrorReceiver();
        receiver.Add(filePath, 9, "MDU004 LocalLinkExists: Target does not exist.");
        receiver.AddWarning(filePath, 1, "MDU006 ReadmeWithoutSubpages: No subpages.");
        var output = new StringWriter();

        new LintResultRenderer(receiver).Render(Array.Empty<MarkdownFile>(), rootPath, pretty: true, color: true, output);

        var result = output.ToString();
        Assert.Contains("Docs/Publish/README.md", result);
        Assert.DoesNotContain(filePath, result);
        Assert.Contains($"  9: \u001b[31merror\u001b[0m MDU004", result);
        Assert.Contains($"  1: \u001b[33mwarning\u001b[0m MDU006", result);
        Assert.Contains("Linter detected 1 \u001b[31merror\u001b[0m and 1 \u001b[33mwarning\u001b[0m.", result);
    }
}
