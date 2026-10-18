namespace MarkdownUtil.Service;

public interface ILinterErrorReceiver
{
    IReadOnlyList<LintDiagnostic> Diagnostics { get; }
    int ErrorCount { get; }
    int WarningCount { get; }
    void Add(string filePath, int lineNumber, string message);
    void AddWarning(string filePath, int lineNumber, string message);
}
