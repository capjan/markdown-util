using System.Text.RegularExpressions;
using Core.Extensions.ParserRelated;
using Core.Parser;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdown.Document.Model;
using Markdown.Document.Model.Impl;

namespace Markdown.Document.Impl;

public class MarkdownDocumentReader : IMarkdownDocumentReader
{
    private static readonly MarkdownPipeline LinkParsingPipeline =
        new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();

    public IMarkdownHeader ReadHeader(IParserInput input)
    {
        var content = input.ReadAll();
        var m = Regex.Match(content, RegExPattern.MarkdownHeader);

        var frontMatter = string.Empty;
        var title = string.Empty;

        if (!m.Success) return new MarkdownHeader(title, frontMatter);
        title = m.Groups["titleName"].Value;
        frontMatter = m.Groups["frontMatter"].Value;

        return new MarkdownHeader(title, frontMatter);
    }

    public IEnumerable<IMarkdownLink> ReadLinks(IParserInput input)
    {
        var content = input.ReadAll();
        var document = Markdig.Parsers.MarkdownParser.Parse(content, LinkParsingPipeline);

        return document.Descendants<LinkInline>().Select(link =>
        {
            var url = link.Url ?? "";
            var literals = link.Descendants<LiteralInline>().ToArray();
            var text = literals.Length == 1 ? literals[0].ToString() : "";
            var isImage = link.IsImage;
            var lineNumber = link.Line + 1;
            return new MarkdownLink(text, url, isImage, lineNumber);
        });
    }

    private bool TryParseLink(IParserInput input, out IMarkdownLink link)
    {
        link = Contants.EmptyLink;
        return false;
    }

}
