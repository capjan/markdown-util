namespace MarkdownUtil.Utils.Graph;

public interface INode<out T> {
    T? Parent { get; }
    T[] Children { get; }
}
