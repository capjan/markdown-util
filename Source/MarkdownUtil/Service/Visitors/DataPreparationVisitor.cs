using System.Text;
using Core.Parser.Special;
using Core.Collections.NodeGraph;
using Markdown.Document;
using MarkdownUtil.Model;

namespace MarkdownUtil.Service.Visitors;

public class DataPreparationVisitor: IVisitor<MarkdownFile>
{
    public TraversalAlgorithm Algorithm => TraversalAlgorithm.DepthFirst;

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
        entity.FrontMatter = header.FrontMatter;
        return true;
    }
}
