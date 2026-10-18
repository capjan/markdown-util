using Core.Collections.NodeGraph.Extensions;
using Markdown.Document.Impl;
using MarkdownUtil.Commands;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class FixCommandTest
{
    [Fact]
    public void FixesReadmeWithoutSubpagesAndKeepsLinksWorking()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var section = Path.Combine(root, "Guide");
        Directory.CreateDirectory(section);
        try
        {
            File.WriteAllText(Path.Combine(root, "README.md"),
                "# Docs\n\n[Page](./Guide/README.md#intro)\n\n[Folder](./Guide/)\n\n[Reference][guide]\n\n[guide]: ./Guide/README.md?view=full#intro\n");
            File.WriteAllText(Path.Combine(section, "README.md"),
                "# Guide\n\n[Asset](./diagram.svg)\n\n[Home](../README.md)\n\n[Anchor](#intro)\n");
            File.WriteAllText(Path.Combine(section, "diagram.svg"), "<svg />");

            var command = new FixCommand(CreateGraphBuilder(), new LinterVisitor(new ConsoleLintErrorReceiver()));
            var result = command.Execute(new VisitorSettings
            {
                RootPath = root,
                SearchPattern = "*.md",
                IncludeHidden = true
            });

            var movedPage = Path.Combine(root, "Guide.md");
            Assert.Equal(0, result);
            Assert.True(File.Exists(movedPage));
            Assert.False(File.Exists(Path.Combine(section, "README.md")));
            Assert.True(File.Exists(Path.Combine(section, "diagram.svg")));

            var rootContent = File.ReadAllText(Path.Combine(root, "README.md"));
            Assert.Contains("[Page](Guide.md#intro)", rootContent);
            Assert.Contains("[Folder](Guide.md)", rootContent);
            Assert.Contains("[guide]: Guide.md?view=full#intro", rootContent);

            var movedContent = File.ReadAllText(movedPage);
            Assert.Contains("[Asset](Guide/diagram.svg)", movedContent);
            Assert.Contains("[Home](README.md)", movedContent);
            Assert.Contains("[Anchor](#intro)", movedContent);

            var graph = CreateGraphBuilder().CreateGraph(new VisitorSettings
            {
                RootPath = root,
                SearchPattern = "*.md",
                IncludeHidden = true
            });
            var receiver = new ConsoleLintErrorReceiver();
            var linter = new LinterVisitor(receiver);
            linter.Prepare(graph);
            graph.Visit(linter);
            Assert.Equal(0, receiver.WarningCount);
            Assert.Equal(0, receiver.ErrorCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MigratesMkDocsBlocksIndexesAndPagesOrdering()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var section = Path.Combine(root, "Guide");
        var nested = Path.Combine(section, "Deep");
        Directory.CreateDirectory(nested);
        try
        {
            File.WriteAllText(Path.Combine(root, "README.md"), "# Docs\n\n[Guide](Guide/index.md)\n");
            File.WriteAllText(Path.Combine(root, ".pages"), "arrange:\n  - README.md\n  - Guide\n");
            File.WriteAllText(Path.Combine(section, "index.md"), "---\ntitle: Overview\n---\n# Guide\n\n??? custom \"Details\"\n\n    Visible content\n\n=== \"First\"\n\n    Tab content\n");
            File.WriteAllText(Path.Combine(nested, "Topic.md"), "# Topic\n");

            var command = new FixCommand(CreateGraphBuilder(), new LinterVisitor(new ConsoleLintErrorReceiver()));
            var result = command.Execute(new VisitorSettings { RootPath = root, SearchPattern = "*.md", IncludeHidden = true });

            Assert.Equal(0, result);
            Assert.False(File.Exists(Path.Combine(root, ".pages")));
            Assert.True(File.Exists(Path.Combine(section, "README.md")));
            Assert.False(File.Exists(Path.Combine(section, "index.md")));
            Assert.Contains("sidebar:", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("order: 1", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("[!NOTE]", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("**Details**", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.DoesNotContain("**\"Details\"**", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("### First", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("[Guide](Guide/README.md)", File.ReadAllText(Path.Combine(root, "README.md")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RemovesMaterialAnnotationMarkersInsideConvertedAlerts()
    {
        const string markdown = "??? note \"Example\"\n\n    Text\n    { .annotate }\n    ```ts\n    const value = 1;\n    ```\n";

        var converted = MarkdownCompatibility.FixMarkdown(markdown);

        Assert.DoesNotContain("{ .annotate }", converted);
        Assert.Contains("const value = 1;", converted);
        Assert.Empty(MarkdownCompatibility.FindIssues(converted));
    }

    [Fact]
    public void SeparatesConsecutiveConvertedAlerts()
    {
        const string markdown = "!!! note \"First\"\n\n    First body\n\n!!! warning \"Second\"\n\n    Second body\n";

        var converted = MarkdownCompatibility.FixMarkdown(markdown);

        Assert.Contains("> First body\n\n> [!WARNING]", converted);
    }

    [Fact]
    public void MigratesBlogAndTagIndexesToStaticLists()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var posts = Path.Combine(root, "blog", "posts");
        Directory.CreateDirectory(posts);
        try
        {
            File.WriteAllText(Path.Combine(root, "README.md"), "# Docs\n");
            File.WriteAllText(Path.Combine(root, "Tags.md"), "# Tags\n\n<!-- material/tags -->\n");
            File.WriteAllText(Path.Combine(root, "blog", "index.md"), "# Blog\n");
            File.WriteAllText(Path.Combine(posts, "First.md"), "---\ntitle: First post\ndate: 2026-01-01\ntags:\n  - Release\n---\n# First\n");
            File.WriteAllText(Path.Combine(posts, "Second.md"), "---\ntitle: Second post\ndate: 2026-01-02\n---\n# Second\n");

            var command = new FixCommand(CreateGraphBuilder(), new LinterVisitor(new ConsoleLintErrorReceiver()));
            var result = command.Execute(new VisitorSettings { RootPath = root, SearchPattern = "*.md", IncludeHidden = true });

            Assert.Equal(0, result);
            var blogIndex = File.ReadAllText(Path.Combine(root, "blog", "README.md"));
            Assert.Contains("posts/First.md", blogIndex);
            Assert.Contains("posts/Second.md", blogIndex);
            Assert.DoesNotContain("posts/README.md", blogIndex);
            Assert.Contains("## Release", File.ReadAllText(Path.Combine(root, "Tags.md")));
            Assert.Contains("blog/posts/First.md", File.ReadAllText(Path.Combine(root, "Tags.md")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PreservesMismatchedSectionTitlesAsSidebarOverrides()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var section = Path.Combine(root, "Guides");
        Directory.CreateDirectory(section);
        try
        {
            File.WriteAllText(Path.Combine(root, "README.md"), "# Docs\n");
            File.WriteAllText(Path.Combine(section, "README.md"), "# Overview\n");
            File.WriteAllText(Path.Combine(section, "Topic.md"), "# Topic\n");

            var command = new FixCommand(CreateGraphBuilder(), new LinterVisitor(new ConsoleLintErrorReceiver()));
            var result = command.Execute(new VisitorSettings { RootPath = root, SearchPattern = "*.md", IncludeHidden = true });

            Assert.Equal(0, result);
            Assert.Contains("title: Overview", File.ReadAllText(Path.Combine(section, "README.md")));
            Assert.Contains("sidebar:", File.ReadAllText(Path.Combine(section, "README.md")));

            var receiver = new ConsoleLintErrorReceiver();
            var graph = CreateGraphBuilder().CreateGraph(new VisitorSettings { RootPath = root, SearchPattern = "*.md", IncludeHidden = true });
            var linter = new LinterVisitor(receiver);
            linter.Prepare(graph);
            graph.Visit(linter);
            Assert.Equal(0, receiver.ErrorCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GraphBuilder CreateGraphBuilder() =>
        new(new DataPreparationVisitor(new MarkdownDocumentReader()));
}
