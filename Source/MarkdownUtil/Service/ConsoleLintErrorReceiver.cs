using System.Net.Sockets;

namespace MarkdownUtil.Service;

public class ConsoleLintErrorReceiver : ILinterErrorReceiver
{
    private readonly List<LintDiagnostic> _diagnostics = new();

    public IReadOnlyList<LintDiagnostic> Diagnostics => _diagnostics;
    public int ErrorCount => _diagnostics.Count(diagnostic => !diagnostic.IsWarning);
    public int WarningCount => _diagnostics.Count(diagnostic => diagnostic.IsWarning);

    public void Add(string filePath, int lineNumber, string message)
    {
        _diagnostics.Add(new LintDiagnostic(filePath, lineNumber, message, IsWarning: false));
    }

    public void AddWarning(string filePath, int lineNumber, string message)
    {
        _diagnostics.Add(new LintDiagnostic(filePath, lineNumber, message, IsWarning: true));
    }
}
