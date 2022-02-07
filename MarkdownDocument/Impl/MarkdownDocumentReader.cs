using System.Text;
using System.Text.RegularExpressions;
using Core.Extensions.ParserRelated;
using Core.Parser;
using MarkdownDocument.Model;

namespace MarkdownDocument.Impl;

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
