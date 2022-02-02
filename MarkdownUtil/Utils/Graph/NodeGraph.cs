namespace MarkdownUtil.Utils.Graph;

public class NodeGraph<T> where T: INode<T>
{
    public IEnumerable<T> RootContent { get; init; } = ArraySegment<T>.Empty;


    public void Visit(IVisitor<T> visitor)
    {
        switch (visitor.Algorithm)
        {
            case GraphTraversalAlgorithm.BreadthFirst:
            case GraphTraversalAlgorithm.Default:
                ExecuteVisitorBreadthFirst(RootContent, visitor, 0);
                break;
            case GraphTraversalAlgorithm.DepthFirst:
                ExecuteVisitorDepthFirst(RootContent, visitor, 0);
                break;
        }
    }

    private bool ExecuteVisitorBreadthFirst(IEnumerable<T> list,
        IVisitor<T> visitor, int graphDepth)
    {
        var usedList = list.ToList();
        foreach (var markdownFile in usedList)
        {
            if (!visitor.Process(markdownFile, graphDepth))
                return false;
        }

        foreach (var markdownFile in usedList)
        {
            if (!ExecuteVisitorBreadthFirst(markdownFile.Children, visitor, graphDepth + 1))
                return false;
        }

        return true;
    }

    private bool ExecuteVisitorDepthFirst(IEnumerable<T> list, IVisitor<T> visitor, int graphDepth)
    {
        foreach (var markdownFile in list)
        {
            if (!visitor.Process(markdownFile, graphDepth))
                return false;

            if (!ExecuteVisitorDepthFirst(markdownFile.Children, visitor, graphDepth + 1))
                return false;
        }

        return true;
    }

}
