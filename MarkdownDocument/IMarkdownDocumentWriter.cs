using MarkdownDocument.Model;

namespace MarkdownDocument;

public interface IMarkdownDocumentWriter
{
    /// <summary>
    /// Writes the given header to the given Markdown Document
    /// </summary>
    /// <param name="fileInfo">file info of the markdown file</param>
    /// <param name="header">header to write</param>
    /// <returns></returns>
    void WriteHeader(FileInfo fileInfo, IMarkdownHeader header);
}