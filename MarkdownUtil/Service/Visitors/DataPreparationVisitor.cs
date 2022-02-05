using System.Text;
using Core.Parser.Special;
using MarkdownDocument;
using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Service.Visitors;

public class DataPreparationVisitor: IVisitor<MarkdownFile>
{
    public GraphTraversalAlgorithm Algorithm => GraphTraversalAlgorithm.DepthFirst;

    private readonly IMarkdownDocumentReader _markdownReader;

    public DataPreparationVisitor(IMarkdownDocumentReader markdownReader)
    {
        _markdownReader = markdownReader;
    }

    public bool Process(MarkdownFile entity, int graphDepth)
    {
        using var reader = new StreamReader(entity.FileInfo.FullName, Encoding.UTF8);
        var input = new ParserInput(reader);
        var header = _markdownReader.ReadHeader(input);
        entity.Title = header.Title;
        return true;
    }
}
