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

    private static GraphBuilder CreateGraphBuilder() =>
        new(new DataPreparationVisitor(new MarkdownDocumentReader()));
}
