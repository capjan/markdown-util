using System.Net.Sockets;
using MarkdownDocument.Impl;
using Spectre.Console;

namespace MarkdownUtil.Service;

public class ConsoleLintErrorReceiver : ILinterErrorReceiver
{
    public int ErrorCount { get; private set; } = 0;

    public void Add(string filePath, int lineNumber, string message)
    {
        AnsiConsole.WriteLine($"{filePath}:{lineNumber}: {message}");
        ErrorCount++;
    }
}
