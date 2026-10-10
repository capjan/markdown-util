using Markdown.Linter;
using Markdown.Linter.Rules.Rule4;

namespace Markdown.Linter.Test;

public class Rule4Test
{
    [Fact]
    public void ExistingRelativeFileWithQueryAndFragmentPasses()
    {
        WithSourceAndTarget((source, _) =>
        {
            var result = Validate(source, "target.md?view=raw#section");

            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void MissingRelativeFileHasStableDiagnosticCode()
    {
        WithSourceAndTarget((source, _) =>
        {
            var result = Validate(source, "missing.md");

            Assert.False(result.IsValid);
            Assert.Contains(RuleCodes.LocalLinkExists, result.ToString());
            Assert.Contains(RuleCodes.LocalLinkExistsName, result.ToString());
        });
    }

    [Fact]
    public void DirectoryWithReadmeIndexPasses()
    {
        WithSourceAndTarget((source, root) =>
        {
            var child = Path.Combine(root, "child");
            Directory.CreateDirectory(child);
            File.WriteAllText(Path.Combine(child, "README.md"), "# Child");

            var result = Validate(source, "child/");

            Assert.True(result.IsValid);
        });
    }

    [Theory]
    [InlineData("#section")]
    [InlineData("https://example.com/page.md")]
    [InlineData("mailto:docs@example.com")]
    [InlineData("//cdn.example.com/image.png")]
    public void NonRelativeDestinationIsIgnored(string destination)
    {
        WithSourceAndTarget((source, _) =>
        {
            var result = Validate(source, destination);

            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void PercentEncodedFileNamePasses()
    {
        WithSourceAndTarget((source, root) =>
        {
            File.WriteAllText(Path.Combine(root, "My Page.md"), "# Page");

            var result = Validate(source, "My%20Page.md");

            Assert.True(result.IsValid);
        });
    }

    private static FluentValidation.Results.ValidationResult Validate(string source, string destination) =>
        new Rule4Validator().Validate(new Rule4Input(source, destination));

    private static void WithSourceAndTarget(Action<string, string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "source.md");
            File.WriteAllText(source, "# Source");
            File.WriteAllText(Path.Combine(root, "target.md"), "# Target");
            action(source, root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
