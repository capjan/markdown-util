using System.Text;
using System.Text.RegularExpressions;
using MarkdownDocument.Model;

namespace MarkdownDocument.Impl;

public class MarkdownDocumentReader : IMarkdownDocumentReader
{
    public IMarkdownHeader ReadHeader(FileInfo fileInfo)
    {
        var content = File.ReadAllText(fileInfo.FullName);
        var m = Regex.Match(content, RegExPattern.MarkdownHeader);

        var title = string.Empty;
        var breadcrumbNavigation = string.Empty;

        if (!m.Success) return new MarkdownHeader(title, breadcrumbNavigation);
        title = m.Groups["title"].Value;
        breadcrumbNavigation = m.Groups["breadcrumbs"].Value;

        return new MarkdownHeader(title, breadcrumbNavigation);
    }
}

public class MarkdownDocumentWriter : IMarkdownDocumentWriter
{


    public void WriteHeader(FileInfo fileInfo, IMarkdownHeader header)
    {
        var content = File.ReadAllText(fileInfo.FullName);
        var sb = new StringBuilder();
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
