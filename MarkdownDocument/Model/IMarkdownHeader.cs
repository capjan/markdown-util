namespace MarkdownDocument.Model;

/// <summary>
/// Expected header of a markdown file
/// </summary>
public interface IMarkdownHeader
{
    /// <summary>Title of the File</summary>
    string Title { get; init; }

    /// <summary>Optional Breadcrumb Navigation Line that provides upward navigation possibilities</summary>
    string BreadcrumbNavigationLine { get; init; }
}