using System.Net.Sockets;
using MarkdownUtil.Model;
using MarkdownUtil.Utils;
using Spectre.Console;

namespace MarkdownUtil.Service;

/// <summary>
/// Main Service to Process Markdown Files
/// </summary>
public class MarkdownService
{

    public MarkdownGraph CreateGraph(DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions searchOptions)
    {
        var list = GetMarkdownFilesAt(null, directoryInfo, searchPattern, searchOptions).ToList();
        return new MarkdownGraph(list);
    }

    private IEnumerable<MarkdownFile> GetMarkdownFilesAt(MarkdownFile? parent, DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions searchOptions)
    {
        return directoryInfo
            .GetFiles(searchPattern, searchOptions)
            .Select(fileInfo => new MarkdownFile(fileInfo, parent))
            .Select(file =>
            {
                file.Children = file.FileInfo.Directory?
                                    .GetDirectories()
                                    .SelectMany(dirInfoOfChildren =>
                                        GetMarkdownFilesAt(file, dirInfoOfChildren, searchPattern, searchOptions)).ToArray()
                                ?? Array.Empty<MarkdownFile>();
                return file;
            })
            .ToArray();
    }

}

public interface ILinterErrorReceiver
{
    void Add(string filePath, int lineNumber, string message);
}

public class ConsoleLintErrorReceiver : ILinterErrorReceiver
{
    public int ErrorCount { get; private set; } = 0;

    public void Add(string filePath, int lineNumber, string message)
    {
        AnsiConsole.WriteLine($"{filePath}:{lineNumber}: {message}");
        ErrorCount++;
    }
}
