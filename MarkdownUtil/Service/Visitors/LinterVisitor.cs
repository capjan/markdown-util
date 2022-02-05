using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Service.Visitors;

public class LinterVisitor: IVisitor<MarkdownFile>
{
    private readonly ILinterErrorReceiver _errorReceiver;

    public LinterVisitor(ILinterErrorReceiver errorReceiver)
    {
        _errorReceiver = errorReceiver;
    }

    public GraphTraversalAlgorithm Algorithm { get; }
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        if (string.IsNullOrWhiteSpace(entity.Title))
        {
            _errorReceiver.Add(entity.FileInfo.FullName, 1, "Page must start with a headline 1 that defines its title. Add a '# pagetitle' to the first line of the document");
        }

        return true;
    }
}
