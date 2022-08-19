namespace MarkdownUtil.Utils.Graph;

public enum GraphTraversalAlgorithm
{
    Default,
    /// <summary>
    /// Uses DepthFirst traversal with preorder. That means visit it first visits a node, than all fist siblings, than the innermost next sibling, etc
    /// </summary>
    DepthFirst,
    /// <summary>
    /// Visits all siblings of a level first, before it goes to the next level.
    /// </summary>
    BreadthFirst,
}
