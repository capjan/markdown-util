using Markdown.Renderer;
using Markdown.Search;
using MarkdownUtil.Model;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class MarkdigVisitorTest
{
    [Fact]
    public void UsesAssetPathsRelativeToEachOutputPageDirectory()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"mdu-render-test-{Guid.NewGuid():N}");
        var docsPath = Path.Combine(rootPath, "Docs");
        var outputPath = Path.Combine(rootPath, "site");
        Directory.CreateDirectory(Path.Combine(docsPath, "Guide"));
        var setupFilePath = Path.Combine(docsPath, "Setup.md");
        var guideFilePath = Path.Combine(docsPath, "Guide", "README.md");
        File.WriteAllText(setupFilePath, "# Setup");
        File.WriteAllText(guideFilePath, "# Guide");

        try
        {
            var setup = new MarkdownFile(new FileInfo(setupFilePath), null) { Title = "Setup" };
            var guide = new MarkdownFile(new FileInfo(guideFilePath), setup) { Title = "Guide" };
            setup.Children = [guide];

            var renderer = new HtmlRenderer();
            renderer.PrepareBasePath(outputPath);
            var visitor = new MarkdigVisitor(docsPath, outputPath, renderer, string.Empty,
                new SearchIndexBuilder(), [setup]);

            visitor.Process(setup, 1);
            visitor.Process(guide, 2);

            var setupHtml = File.ReadAllText(Path.Combine(outputPath, "Setup.html"));
            var guideHtml = File.ReadAllText(Path.Combine(outputPath, "Guide", "index.html"));
            Assert.Contains("href=\"css/main.css\"", setupHtml);
            Assert.Contains("data-search-index=\"search-index.json\"", setupHtml);
            Assert.Contains("src=\"js/search.js\"", setupHtml);
            Assert.Contains("href=\"../css/main.css\"", guideHtml);
            Assert.Contains("data-search-index=\"../search-index.json\"", guideHtml);
            Assert.Contains("src=\"../js/search.js\"", guideHtml);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }
}
