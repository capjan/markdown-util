namespace MarkdownUtil.Service;

public interface ILinterErrorReceiver
{
    int ErrorCount { get; }
    void Add(string filePath, int lineNumber, string message);
}