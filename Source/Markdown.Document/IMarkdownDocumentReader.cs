using Core.Parser;
using Markdown.Document.Model;

namespace Markdown.Document;

public interface IMarkdownDocumentReader
{
    /// <summary>
    /// Reads the expected headline of the given Markdown Document
    /// </summary>
    /// <param name="input">input of the Parser</param>
    /// <returns></returns>
    IMarkdownHeader ReadHeader(IParserInput input);
}
