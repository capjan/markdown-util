﻿using System.Text;
using System.Text.RegularExpressions;
using Core.Extensions.ParserRelated;
using Core.Parser;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdown.Document.Model;
using Markdown.Document.Model.Impl;

namespace Markdown.Document.Impl;

public class MarkdownDocumentReader : IMarkdownDocumentReader
{
    public IMarkdownHeader ReadHeader(IParserInput input)
    {
        var content = input.ReadAll();
        var m = Regex.Match(content, RegExPattern.MarkdownHeader);

        var frontMatter = string.Empty;
        var title = string.Empty;
        var breadcrumbNavigation = string.Empty;

        if (!m.Success) return new MarkdownHeader(title, breadcrumbNavigation, frontMatter);
        title = m.Groups["titleName"].Value;
        breadcrumbNavigation = m.Groups["breadcrumbs"].Value;
        frontMatter = m.Groups["frontMatter"].Value;

        return new MarkdownHeader(title, breadcrumbNavigation, frontMatter);
    }

    public IEnumerable<IMarkdownLink> ReadLinks(IParserInput input)
    {
        var content = input.ReadAll();
        var document = Markdig.Parsers.MarkdownParser.Parse(content);

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

public class MarkdownDocumentWriter : IMarkdownDocumentWriter
{
    
    public void WriteHeader(FileInfo fileInfo, IMarkdownHeader header)
    {
        var content = File.ReadAllText(fileInfo.FullName);
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(header.FrontMatter))
        {
            sb.AppendLine(header.FrontMatter);
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(header.Title))
        {
            sb.Append("# ");
            sb.AppendLine(header.Title.Trim());
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(header.BreadcrumbNavigationLine))
        {
            sb.AppendLine(header.BreadcrumbNavigationLine.Trim());
            sb.AppendLine();
        }

        var replacement = sb.ToString();
        var replacedContent = Regex.Replace(content, RegExPattern.MarkdownHeader, replacement);
        var backupFileName = Path.ChangeExtension(fileInfo.FullName, ".md.bak");
        File.Move(fileInfo.FullName, backupFileName);
        File.WriteAllText(fileInfo.FullName, replacedContent);
        File.Delete(backupFileName);
    }
}
