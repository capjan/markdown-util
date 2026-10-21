using Markdown.Document.Impl;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class GraphBuilderHiddenTest
{
    [Theory]
    [InlineData(false, new[] { "README.md" })]
    [InlineData(true, new[] { ".dot.md", "README.md", "README.md" })]
    public void HiddenFlagAppliesToFilesAndFolders(bool includeHidden, string[] expected)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, ".secret"));
            File.WriteAllText(Path.Combine(root, "README.md"), "# Home\n");
            File.WriteAllText(Path.Combine(root, ".dot.md"), "# Dot\n");
            File.WriteAllText(Path.Combine(root, ".secret", "README.md"), "# Secret\n");

            var graph = new GraphBuilder(new DataPreparationVisitor(new MarkdownDocumentReader()))
                .CreateGraph(new VisitorSettings { RootPath = root, SearchPattern = "*.md", IncludeHidden = includeHidden });

            var names = Flatten(graph).Select(page => page.FileInfo.Name).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(expected, names);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static IEnumerable<MarkdownFile> Flatten(IEnumerable<MarkdownFile> pages) =>
        pages.SelectMany(page => new[] { page }.Concat(Flatten(page.Children ?? [])));
}
