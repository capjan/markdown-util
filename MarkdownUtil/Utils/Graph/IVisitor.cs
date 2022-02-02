namespace MarkdownUtil.Utils.Graph;

public interface IVisitor<in T>
{
    GraphTraversalAlgorithm Algorithm { get; }
    bool Process(T entity, int graphDepth);
}
