using System.Linq;
using System.Reflection;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Xunit;

namespace Markdown.Document.Test;

public class UnitTestMarkdig
{
    [Fact]
    public void ParseSimpleDocument()
    {
        var simpleDocument = EmbeddedResources.Strings.SimpleDocument;
        var document = Markdig.Parsers.MarkdownParser.Parse(simpleDocument);

        var allLinks = document.Descendants<LinkInline>().ToList();
        
        Assert.NotEmpty(allLinks);
        var link = allLinks[0];
        var linkText = link.ToString();
        var title = link.Title;
        var label = link.Label;
        var url = link.Url;
        var isImage = link.IsImage;
        var reference = link.Reference;
        var linkIsShortcut = link.IsShortcut;
        var localLabel = link.LocalLabel;
        var unescaped = link.UnescapedTitle;
        var literals = link.Descendants<LiteralInline>().ToArray();
        var text = literals[0].ToString();
        Assert.Equal("Project Website", text);
        Assert.Equal("https://github.com/capjan/markdown-util", url);

        

    }
    
}