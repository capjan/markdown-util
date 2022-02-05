using Core.Parser;
using MarkdownDocument.Model;

namespace MarkdownDocument;

public interface IMarkdownDocumentReader
{
    /// <summary>
    /// Reads the expected headline of the given Markdown Document
    /// </summary>
    /// <param name="input">input of the Parser</param>
    /// <returns></returns>
    IMarkdownHeader ReadHeader(IParserInput input);
}
