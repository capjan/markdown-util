using System.IO;
using Core.Parser.Special;
using MarkdownDocument.Impl;
using Xunit;

namespace MarkdownDocumentTest;

public class UnitTest1
{
    [Theory]
    [InlineData("# Home\n\n## Themen\n\nOtherText", "Home", "")]
    [InlineData("  # Home\n\n## Themen\n\nOtherText", "Home", "")]
    [InlineData("", "", "")]
    [InlineData("# h1 ( bla )\n", "h1 ( bla )", "")]
    [InlineData("# h1 /|\\.,.<>1234567890$#()\r\n", "h1 /|\\.,.<>1234567890$#()", "")]
    [InlineData("# Home\n\nHome\n\nOther Text", "Home", "Home")]
    public void Test1(string input, string expectedTitle, string expectedBreadcrumbNavigationLine)
    {
        var sut = new MarkdownDocumentReader();
        var sr = new StringReader(input);
        var inputReader = new ParserInput(sr);
        var header = sut.ReadHeader(inputReader);
        Assert.Equal(expectedTitle, header.Title);
        Assert.Equal(expectedBreadcrumbNavigationLine, header.BreadcrumbNavigationLine);
    }
}
