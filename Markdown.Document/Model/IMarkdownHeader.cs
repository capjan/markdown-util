namespace Markdown.Document.Model;

/// <summary>
/// Expected header of a markdown file
/// </summary>
public interface IMarkdownHeader
{
    /// <summary>
    /// Optional Front Matter Area
    /// </summary>
    string FrontMatter { get; init; }

    /// <summary>Title of the File</summary>
    string Title { get; init; }

    /// <summary>Optional Breadcrumb Navigation Line that provides upward navigation possibilities</summary>
    string BreadcrumbNavigationLine { get; init; }
}
