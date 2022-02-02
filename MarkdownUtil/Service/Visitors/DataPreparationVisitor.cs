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
        var header = _markdownReader.ReadHeader(entity.FileInfo);
        entity.Title = header.Title;
        return true;
    }
}
