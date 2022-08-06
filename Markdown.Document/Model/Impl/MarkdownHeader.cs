namespace Markdown.Document.Model.Impl;

/// <summary>
/// Expected Header of a Markdown File
/// </summary>
/// <param name="Title">Title of the File</param>
/// <param name="BreadcrumbNavigationLine">Optional Breadcrumb Navigation Line</param>
public readonly record struct MarkdownHeader(
    string Title,
    string BreadcrumbNavigationLine,
    string FrontMatter
) : IMarkdownHeader;
