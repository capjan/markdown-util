using Markdown.Linter.Rules.Rule2;

namespace MarkdownUtilTest;

public class Rule2Test
{
    [Theory]
    [InlineData("Rule 2: Directory name must match the title of a Markdown", "rule2")]
    [InlineData("Rule 2: Directory name must match the title of a Markdown", "Rule2")]
    [InlineData("Rule 2: Directory name must match the title of a Markdown", "Rule-2")]
    public void TestValidRuleValidations(string title, string directoryName)
    {
        var sut = new Rule2Validator();
        var input = new Rule2Input(title, directoryName);
        var result = sut.Validate(input);
        Assert.True(result.IsValid);
    }

}