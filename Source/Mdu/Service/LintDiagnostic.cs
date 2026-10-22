namespace Mdu.Service;

public sealed record LintDiagnostic(string FilePath, int LineNumber, string Message, bool IsWarning);
