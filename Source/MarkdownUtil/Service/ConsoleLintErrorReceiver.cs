using System.Net.Sockets;
namespace MarkdownUtil.Service;

public class ConsoleLintErrorReceiver : ILinterErrorReceiver
{
    public int ErrorCount { get; private set; } = 0;
    public int WarningCount { get; private set; } = 0;

    public void Add(string filePath, int lineNumber, string message)
    {
        Console.WriteLine($"{filePath}:{lineNumber}: {message}");
        ErrorCount++;
    }

    public void AddWarning(string filePath, int lineNumber, string message)
    {
        Console.WriteLine($"{filePath}:{lineNumber}: warning {message}");
        WarningCount++;
    }
}
