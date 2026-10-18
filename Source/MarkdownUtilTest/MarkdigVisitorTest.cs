using Markdown.Renderer;
using Markdown.Search;
using MarkdownUtil.Model;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtilTest;

public class MarkdigVisitorTest
{
    [Fact]
    public void RemovesIconShortcodesFromTitlesAndEscapesEditPagePaths()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"mdu-render-test-{Guid.NewGuid():N}");
        var docsPath = Path.Combine(rootPath, "Docs");
        var outputPath = Path.Combine(rootPath, "site");
        var pageDirectory = Path.Combine(docsPath, "A B");
        Directory.CreateDirectory(pageDirectory);
        var pagePath = Path.Combine(pageDirectory, "README.md");
        File.WriteAllText(pagePath, "# :simple-rider: Rider\n");

        try
        {
            var page = new MarkdownFile(new FileInfo(pagePath), null)
            {
                Title = ":simple-rider: Rider",
                SidebarTitle = ":simple-rider: Rider"
            };
            var renderer = new HtmlRenderer();
            renderer.PrepareBasePath(outputPath);
            var visitor = new MarkdigVisitor(docsPath, outputPath, renderer,
                "https://github.com/capjan/Wiki/edit/main/docs", new SearchIndexBuilder(), [page]);

            visitor.Process(page, 0);

            var html = File.ReadAllText(Path.Combine(outputPath, "A B", "index.html"));
            Assert.Contains("<title>Rider</title>", html);
            Assert.Contains("A%20B/README.md", html);
            Assert.DoesNotContain(":simple-rider:", html);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void UsesAssetPathsRelativeToEachOutputPageDirectory()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"mdu-render-test-{Guid.NewGuid():N}");
        var docsPath = Path.Combine(rootPath, "Docs");
        var outputPath = Path.Combine(rootPath, "site");
        Directory.CreateDirectory(Path.Combine(docsPath, "Guide"));
        Directory.CreateDirectory(Path.Combine(docsPath, "Setup", "_assets"));
        var setupFilePath = Path.Combine(docsPath, "Setup.md");
        var guideFilePath = Path.Combine(docsPath, "Guide", "README.md");
        var setupImagePath = Path.Combine(docsPath, "Setup", "_assets", "photo.svg");
        var imagePath = Path.Combine(docsPath, "Guide", "photo.webp");
        File.WriteAllText(setupFilePath,
            "# Setup\n\n![Photo](./Setup/_assets/photo.svg)\n\n[JSON](./Setup/_assets/output.json)\n\n[PDF](./Setup/_assets/Safety%20Data.pdf)");
        File.WriteAllText(setupImagePath, "<svg></svg>");
        File.WriteAllText(Path.Combine(docsPath, "Setup", "_assets", "output.json"), "{}");
        File.WriteAllText(Path.Combine(docsPath, "Setup", "_assets", "Safety Data.pdf"), "pdf");
        File.WriteAllText(guideFilePath, "# Guide\n\n![Photo](./photo.webp)");
        File.WriteAllBytes(imagePath, [0x52, 0x49, 0x46, 0x46]);

        try
        {
            var setup = new MarkdownFile(new FileInfo(setupFilePath), null) { Title = "Setup" };
            var guide = new MarkdownFile(new FileInfo(guideFilePath), setup)
            {
                Title = "Overview",
                SidebarTitle = ".NET",
                Description = "Guide metadata",
                Icon = "material-home"
            };
            setup.Children = [guide];

            var renderer = new HtmlRenderer();
            renderer.PrepareBasePath(outputPath);
            var visitor = new MarkdigVisitor(docsPath, outputPath, renderer, string.Empty,
                new SearchIndexBuilder(), [setup]);

            visitor.Process(setup, 1);
            new LocalAssetsCopy(docsPath, outputPath).Process(setup, 1);
            new LocalAssetsCopy(docsPath, outputPath).Process(guide, 2);
            visitor.Process(guide, 2);

            var setupHtml = File.ReadAllText(Path.Combine(outputPath, "Setup.html"));
            var guideHtml = File.ReadAllText(Path.Combine(outputPath, "Guide", "index.html"));
            Assert.Contains("href=\"css/main.css\"", setupHtml);
            Assert.Contains("data-search-index=\"search-index.json\"", setupHtml);
            Assert.Contains("src=\"js/search.js\"", setupHtml);
            Assert.Contains("href=\"../css/main.css\"", guideHtml);
            Assert.Contains("data-search-index=\"../search-index.json\"", guideHtml);
            Assert.Contains("src=\"../js/search.js\"", guideHtml);
            Assert.Contains(">.NET</a>", guideHtml);
            Assert.Contains("<title>Overview</title>", guideHtml);
            Assert.Contains("name=\"description\" content=\"Guide metadata\"", guideHtml);
            Assert.Contains("class=\"icon\"", guideHtml);
            Assert.True(File.Exists(Path.Combine(outputPath, "Guide", "photo.webp")));
            Assert.True(File.Exists(Path.Combine(outputPath, "Setup", "_assets", "photo.svg")));
            Assert.True(File.Exists(Path.Combine(outputPath, "Setup", "_assets", "output.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "Setup", "_assets", "Safety Data.pdf")));
            Assert.Contains("../Setup.html", File.ReadAllText(Path.Combine(outputPath, "Setup", "index.html")));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }
}
