using Markdown.Linter;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class LinterVisitorTest
{
    [Fact]
    public void MissingTitleHasStableDiagnosticCode()
    {
        WithPage("Overview.md", "Plain text without a heading", string.Empty, 0, errors =>
        {
            var error = Assert.Single(errors);
            Assert.Equal(RuleCodes.HeadingRequired, CodeOf(error));
        });
    }

    [Fact]
    public void ExistingTitlePasses()
    {
        WithPage("Overview.md", "# Overview", "Overview", 0, errors => Assert.Empty(errors));
    }

    [Fact]
    public void MismatchedReadmeFolderHasStableDiagnosticCode()
    {
        WithPage(Path.Combine("wrong-folder", "README.md"), "# Installation", "Installation", 1, errors =>
        {
            var error = Assert.Single(errors);
            Assert.Equal(RuleCodes.DirectoryTitleMatch, CodeOf(error));
        });
    }

    [Fact]
    public void MissingLocalLinkHasStableDiagnosticCodeAndSourceLine()
    {
        WithPage("Overview.md", "# Overview\n\n[Missing](missing.md)", "Overview", 0, errors =>
        {
            var error = Assert.Single(errors);
            Assert.Equal(3, error.LineNumber);
            Assert.Equal(RuleCodes.LocalLinkExists, CodeOf(error));
        });
    }

    [Fact]
    public void MissingImageHasStableDiagnosticCode()
    {
        WithPage("Overview.md", "# Overview\n\n![Missing](missing.png)", "Overview", 0, errors =>
        {
            var error = Assert.Single(errors);
            Assert.Equal(RuleCodes.LocalLinkExists, CodeOf(error));
        });
    }

    [Fact]
    public void ExistingImagePasses()
    {
        WithPage("Overview.md", "# Overview\n\n![Diagram](diagram.png)", "Overview", 0, errors => Assert.Empty(errors),
            setup: root => File.WriteAllText(Path.Combine(root, "diagram.png"), "image"));
    }

    [Fact]
    public void CasingMismatchDoesNotAlsoReportMissingTarget()
    {
        WithPage(Path.Combine("docs", "Overview.md"), "# Overview\n\n[Target](Target.md)", "Overview", 0, errors =>
        {
            Assert.Contains(errors, error => CodeOf(error) == RuleCodes.LinkCasing);
            Assert.DoesNotContain(errors, error => CodeOf(error) == RuleCodes.LocalLinkExists);
        }, setup: root =>
        {
            var docs = Path.Combine(root, "docs");
            Directory.CreateDirectory(docs);
            File.WriteAllText(Path.Combine(docs, "target.md"), "# Target");
        });
    }

    private static string CodeOf(ReceivedError error) =>
        error.Message.Split(':', 2)[0].Split(' ', 2)[0];

    private static void WithPage(
        string relativePath,
        string contents,
        string title,
        int depth,
        Action<List<ReceivedError>> assert,
        Action<string>? setup = null)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            setup?.Invoke(root);
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
            var page = new MarkdownFile(new FileInfo(path), null) { Title = title };
            var receiver = new RecordingLintErrorReceiver();

            new LinterVisitor(receiver).Process(page, depth);

            assert(receiver.Errors);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed record ReceivedError(string FilePath, int LineNumber, string Message);

    private sealed class RecordingLintErrorReceiver : ILinterErrorReceiver
    {
        public List<ReceivedError> Errors { get; } = new();
        public List<ReceivedError> Warnings { get; } = new();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;

        public void Add(string filePath, int lineNumber, string message) =>
            Errors.Add(new ReceivedError(filePath, lineNumber, message));

        public void AddWarning(string filePath, int lineNumber, string message) =>
            Warnings.Add(new ReceivedError(filePath, lineNumber, message));
    }
}
