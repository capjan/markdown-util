namespace StaticCode.Diagrams;

/// <summary>
/// Raised when diagram source uses syntax outside the supported subset.
/// </summary>
public sealed class DiagramSyntaxException : Exception
{
    public DiagramSyntaxException(string message, int line)
        : base($"Line {line}: {message}")
    {
        Line = line;
    }

    public int Line { get; }
}
