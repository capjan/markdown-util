namespace MarkdownDocument.Errors;

public class MarkdownDocumentException : Exception
{
    public MarkdownDocumentException(string message, string? filePath = default, Exception? innerException = default) : base(message, innerException)
    {
       FilePath = filePath;
    }

    public string? FilePath { get; }
}
