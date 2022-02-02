using MarkdownDocument.Model;

namespace MarkdownDocument;

public interface IMarkdownDocumentReader
{
    /// <summary>
    /// Reads the expected headline of the given Markdown Document
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    IMarkdownHeader ReadHeader(FileInfo fileInfo);
}
