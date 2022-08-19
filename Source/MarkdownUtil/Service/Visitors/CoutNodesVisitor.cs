using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Service.Visitors;

public class CountNodesVisitor<T> : IVisitor<T>
{
    public GraphTraversalAlgorithm Algorithm => GraphTraversalAlgorithm.Default;
    public int NodeCount { get; private set; }

    public bool Process(T entity, int graphDepth)
    {
        NodeCount++;
        return true;
    }

}