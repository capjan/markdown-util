using System.Diagnostics;
using Markdown.Document.Impl;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class RepositoryImportTest
{
    [Fact]
    public void ImportsRepositoryTreeAndExpandsLocalChildImports()
    {
        var root = CreateTemporaryDirectory();
        var firstRepository = Path.Combine(root, "first-repository");
        var secondRepository = Path.Combine(root, "second-repository");
        var docsPath = Path.Combine(root, "docs");
        Directory.CreateDirectory(Path.Combine(firstRepository, "Docs"));
        Directory.CreateDirectory(Path.Combine(secondRepository, "Docs"));
        Directory.CreateDirectory(docsPath);
        var outerUrl = "https://github.com/example/first/tree/main/Docs";
        var nestedUrl = "https://github.com/example/second/tree/main/Docs";
        File.WriteAllText(Path.Combine(docsPath, "README.md"), $$"""
            ---
            title: Combined API
            mdu:
              import:
                source: {{outerUrl}}
            ---
            """);
        File.WriteAllText(Path.Combine(docsPath, "Nested.md"), $$"""
            ---
            title: Nested API
            mdu:
              import:
                source: {{nestedUrl}}
            ---
            """);
        File.WriteAllText(Path.Combine(firstRepository, "Docs", "README.md"), "# Remote API\n\nRemote introduction.\n");
        File.WriteAllText(Path.Combine(secondRepository, "Docs", "README.md"), "# Nested Remote API\n\nNested remote introduction.\n");

        try
        {
            var resolver = new FakeSourceResolver(
                (outerUrl, CreateResolvedSource(firstRepository, "first")),
                (nestedUrl, CreateResolvedSource(secondRepository, "second")));
            var graph = CreateGraphBuilder(resolver).CreateGraph(new VisitorSettings { RootPath = docsPath });

            var rootPage = Assert.Single(graph);
            Assert.Equal("Combined API", rootPage.Title);
            Assert.Contains("Remote introduction.", rootPage.ReadMarkdown());
            Assert.Equal("README.md", rootPage.SiteRelativePath);

            var nestedPage = Assert.Single(rootPage.Children);
            Assert.Equal("Nested API", nestedPage.Title);
            Assert.Contains("Nested remote introduction.", nestedPage.ReadMarkdown());
            Assert.Equal(Path.Combine("Nested", "README.md"), nestedPage.SiteRelativePath);
            Assert.Equal(2, resolver.ResolveCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsMarkdownBodyOnImportMarker()
    {
        var root = CreateTemporaryDirectory();
        var repository = Path.Combine(root, "repository");
        Directory.CreateDirectory(Path.Combine(repository, "Docs"));
        var sourceUrl = "https://github.com/example/repository/tree/main/Docs";
        File.WriteAllText(Path.Combine(root, "README.md"), $"---\nmdu:\n  import:\n    source: {sourceUrl}\n---\nThis would be lost.\n");
        File.WriteAllText(Path.Combine(repository, "Docs", "README.md"), "# Imported\n");

        try
        {
            var resolver = new FakeSourceResolver((sourceUrl, CreateResolvedSource(repository, "repository")));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                CreateGraphBuilder(resolver).CreateGraph(new VisitorSettings { RootPath = root }));
            Assert.Contains("must contain front matter only", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RequiresASourceForAnImportDeclaration()
    {
        var root = CreateTemporaryDirectory();
        File.WriteAllText(Path.Combine(root, "README.md"), "---\nmdu:\n  import: {}\n---\n");

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                CreateGraphBuilder(new FakeSourceResolver()).CreateGraph(new VisitorSettings { RootPath = root }));
            Assert.Contains("mdu.import.source", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsOpenApiSpecificationsOutsideImportedRepository()
    {
        var root = CreateTemporaryDirectory();
        var repository = Path.Combine(root, "repository");
        var docs = Path.Combine(repository, "Docs");
        Directory.CreateDirectory(docs);
        var sourceUrl = "https://github.com/example/repository/tree/main/Docs";
        File.WriteAllText(Path.Combine(root, "README.md"), $"---\nmdu:\n  import:\n    source: {sourceUrl}\n---\n");
        File.WriteAllText(Path.Combine(docs, "README.md"), "# Remote API\n");
        File.WriteAllText(Path.Combine(docs, "Details.md"), "---\nopenapi: ../../../../outside.yaml\n---\n# Details\n");

        try
        {
            var resolver = new FakeSourceResolver((sourceUrl, CreateResolvedSource(repository, "repository")));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                CreateGraphBuilder(resolver).CreateGraph(new VisitorSettings { RootPath = root }));
            Assert.Contains("inside the imported repository", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RejectsPagesThatMapToTheSameSitePath()
    {
        var root = CreateTemporaryDirectory();
        var repository = Path.Combine(root, "repository");
        Directory.CreateDirectory(Path.Combine(repository, "Docs"));
        var sourceUrl = "https://github.com/example/repository/tree/main/Docs";
        File.WriteAllText(Path.Combine(root, "README.md"), "# Home\n");
        File.WriteAllText(Path.Combine(root, "index.md"), $"---\nmdu:\n  import:\n    source: {sourceUrl}\n---\n");
        File.WriteAllText(Path.Combine(repository, "Docs", "README.md"), "# Imported\n");

        try
        {
            var resolver = new FakeSourceResolver((sourceUrl, CreateResolvedSource(repository, "repository")));
            var exception = Assert.Throws<InvalidOperationException>(() =>
                CreateGraphBuilder(resolver).CreateGraph(new VisitorSettings { RootPath = root }));
            Assert.Contains("More than one Markdown page maps to 'README.md'", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SkipsAssetsThatWouldBeRenderedOutsideTheSiteRoot()
    {
        var root = CreateTemporaryDirectory();
        var repository = Path.Combine(root, "repository");
        var docs = Path.Combine(repository, "Docs");
        var output = Path.Combine(root, "site");
        Directory.CreateDirectory(docs);
        var sourceAsset = Path.Combine(repository, "outside.png");
        File.WriteAllText(sourceAsset, "asset");
        var pagePath = Path.Combine(docs, "README.md");
        File.WriteAllText(pagePath, "![Outside](../outside.png)\n");
        var page = new MarkdownFile(new FileInfo(pagePath), null)
        {
            SiteRelativePath = "README.md",
            SourceRootPath = repository
        };

        try
        {
            new LocalAssetsCopy(docs, output).Process(page, 0);

            Assert.True(File.Exists(sourceAsset));
            Assert.False(File.Exists(Path.Combine(root, "outside.png")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolvesSlashBranchesAndChecksOutSymlinksAsRegularFiles()
    {
        if (OperatingSystem.IsWindows()) return;

        var root = CreateTemporaryDirectory();
        var sourceRepository = Path.Combine(root, "source");
        var docs = Path.Combine(sourceRepository, "Docs");
        Directory.CreateDirectory(docs);
        var secretPath = Path.Combine(root, "secret.txt");
        File.WriteAllText(secretPath, "secret contents");
        File.WriteAllText(Path.Combine(docs, "README.md"), "# Imported\n");
        File.CreateSymbolicLink(Path.Combine(docs, "secret.md"), secretPath);
        RunGit(sourceRepository, ["init", "-b", "feature/docs"]);
        RunGit(sourceRepository, ["config", "user.email", "mdu-tests@example.com"]);
        RunGit(sourceRepository, ["config", "user.name", "MDU tests"]);
        RunGit(sourceRepository, ["add", "."]);
        RunGit(sourceRepository, ["commit", "-m", "test source"]);
        var commit = RunGit(sourceRepository, ["rev-parse", "HEAD"]).Trim();

        try
        {
            var resolver = new GitHubTreeSourceResolver(
                new LocalRepositoryGitRunner(sourceRepository, commit, "feature/docs"),
                Path.Combine(root, "cache"));
            var resolved = resolver.Resolve("https://github.com/example/repository/tree/feature/docs/Docs");
            var checkedOutSymlink = new FileInfo(Path.Combine(resolved.SourceFolderPath, "secret.md"));

            Assert.Equal("feature/docs", resolved.ReferenceName);
            Assert.Equal("Docs", resolved.SourcePath);
            Assert.Null(checkedOutSymlink.LinkTarget);
            Assert.NotEqual("secret contents", File.ReadAllText(checkedOutSymlink.FullName));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GraphBuilder CreateGraphBuilder(IGitHubTreeSourceResolver resolver) =>
        new(new DataPreparationVisitor(new MarkdownDocumentReader()), resolver);

    private static ResolvedGitHubTreeSource CreateResolvedSource(string repositoryPath, string repositoryName) =>
        new(
            $"https://github.com/example/{repositoryName}.git",
            $"https://github.com/example/{repositoryName}",
            "main",
            "Docs",
            repositoryPath,
            Path.Combine(repositoryPath, "Docs"));

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mdu-import-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RunGit(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("git") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }

    private sealed class FakeSourceResolver(params (string Url, ResolvedGitHubTreeSource Source)[] sources)
        : IGitHubTreeSourceResolver
    {
        private readonly Dictionary<string, ResolvedGitHubTreeSource> _sources =
            sources.ToDictionary(source => source.Url, source => source.Source, StringComparer.Ordinal);

        public int ResolveCount { get; private set; }

        public ResolvedGitHubTreeSource Resolve(string sourceUrl)
        {
            ResolveCount++;
            return _sources[sourceUrl];
        }
    }

    private sealed class LocalRepositoryGitRunner(string sourceRepository, string commit, string branch)
        : IGitCommandRunner
    {
        public string Run(string? workingDirectory, IReadOnlyList<string> arguments)
        {
            if (arguments[0] == "ls-remote")
                return $"{commit}\trefs/heads/{branch}\n";

            var actualArguments = arguments.ToArray();
            if (actualArguments[0] == "clone") actualArguments[6] = sourceRepository;
            return RunGit(workingDirectory ?? Environment.CurrentDirectory, actualArguments);
        }
    }
}
