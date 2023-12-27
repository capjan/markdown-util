namespace Markdown.Document.Model;

public interface IMarkdownLink
{
    string Name { get; }
    string Destination { get; }
    bool IsImageLink { get; }
    int LineNumber { get; }
}