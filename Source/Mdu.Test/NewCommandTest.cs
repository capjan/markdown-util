using Markdown.Document.Impl;
using Mdu.Commands;
using Mdu.Commands.Settings;
using Mdu.Service;
using Mdu.Service.Renderer;
using Mdu.Service.Visitors;

namespace Mdu.Test;

[CollectionDefinition("Process state", DisableParallelization = true)]
public sealed class ProcessStateCollectionDefinition
{
}

[Collection("Process state")]
public sealed class NewCommandTest : IDisposable
{
    private readonly string _originalDirectory = Environment.CurrentDirectory;
    private readonly string _root = CreateTemporaryDirectory();

    public NewCommandTest() => Environment.CurrentDirectory = _root;

    public void Dispose()
    {
        Environment.CurrentDirectory = _originalDirectory;
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void CreatesNestedProjectAndPrintsNextSteps()
    {
        var (exitCode, output, error) = Capture(() => NewCommand.Execute("nested/my-docs"));
        var pagePath = Path.Combine(_root, "nested", "my-docs", "docs", "README.md");

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.Equal("# Documentation\n\nStart writing your documentation here.\n", File.ReadAllText(pagePath));
        Assert.Contains("cd \"nested/my-docs\"", output.Replace('\\', '/'));
        Assert.Contains("mdu serve", output);
    }

    [Fact]
    public void CreatesProjectInCurrentDirectoryWhenTargetIsOmitted()
    {
        var (exitCode, output, error) = Capture(() => NewCommand.Execute(null));

        Assert.Equal(0, exitCode);
        Assert.Empty(error);
        Assert.True(File.Exists(Path.Combine(_root, "docs", "README.md")));
        Assert.Contains("Next: mdu serve", output);
    }

    [Theory]
    [InlineData("Docs", "Guide.md", "# Guide\n")]
    [InlineData("docs", "README.md", "# Existing\n")]
    public void RefusesAnExistingDocumentationFolderWithoutChangingIt(string folder, string file, string content)
    {
        var docs = Path.Combine(_root, folder);
        Directory.CreateDirectory(docs);
        var existingFile = Path.Combine(docs, file);
        File.WriteAllText(existingFile, content);

        var (exitCode, _, error) = Capture(() => NewCommand.Execute("."));

        Assert.Equal(1, exitCode);
        Assert.Contains("documentation folder already exists", error);
        Assert.Equal(content, File.ReadAllText(existingFile));
        Assert.Single(Directory.EnumerateFiles(docs));
    }

    [Fact]
    public void ReportsWhenTheTargetPathIsAFile()
    {
        File.WriteAllText("file.txt", "keep me");

        var (exitCode, _, error) = Capture(() => NewCommand.Execute("file.txt"));

        Assert.Equal(1, exitCode);
        Assert.Contains("The target path is a file:", error);
        Assert.Equal("keep me", File.ReadAllText("file.txt"));
        Assert.Single(Directory.EnumerateFileSystemEntries(_root));
    }

    [Fact]
    public void SuggestsNewWhenTheDefaultDocsFolderIsMissing()
    {
        var (exitCode, output, error) = Capture(() => CreateLintCommand().Execute(new VisitorSettings()));

        Assert.Equal(1, exitCode);
        Assert.Empty(output);
        Assert.Equal(
            "No ./docs folder found. Run 'mdu new .' to create a starter project, or specify a Markdown folder in the command.\n",
            error.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void AsksForAnExplicitFolderWhenMultipleDocsFoldersExist()
    {
        Directory.CreateDirectory("docs");
        Directory.CreateDirectory("Docs");
        if (Directory.EnumerateDirectories(_root).Count() < 2) return;

        var (exitCode, output, error) = Capture(() => CreateLintCommand().Execute(new VisitorSettings()));

        Assert.Equal(1, exitCode);
        Assert.Empty(output);
        Assert.Equal(
            "Multiple folders named docs found. Specify the desired Markdown folder in the command.\n",
            error.ReplaceLineEndings("\n"));
    }

    private static LintCommand CreateLintCommand()
    {
        var receiver = new ConsoleLintErrorReceiver();
        var graphBuilder = new GraphBuilder(new DataPreparationVisitor(new MarkdownDocumentReader()));
        return new LintCommand(graphBuilder, new LinterVisitor(receiver), new LintResultRenderer(receiver));
    }

    private static (int ExitCode, string Output, string Error) Capture(Func<int> action)
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);
        try
        {
            var exitCode = action();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mdu-new-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
