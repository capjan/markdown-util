namespace Markdown.Document.Model.Impl;

/// <summary>
/// Expected Header of a Markdown File
/// </summary>
/// <param name="Title">Title of the File</param>
public readonly record struct MarkdownHeader(
    string Title,
    string FrontMatter
) : IMarkdownHeader;
