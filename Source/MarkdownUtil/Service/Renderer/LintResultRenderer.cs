namespace MarkdownUtil.Service.Renderer;

public class LintResultRenderer
{
    private readonly ILinterErrorReceiver _receiver;

    public LintResultRenderer(ILinterErrorReceiver receiver)
    {
        this._receiver = receiver;
    }

    public int Render()
    {
        var errorCount = _receiver.ErrorCount;
        if (errorCount == 1)
        {
            Console.WriteLine($"Linter detected {errorCount} error.");
            return 1;
        }
        if (errorCount > 1)
        {
            Console.WriteLine($"Linter detected {errorCount} errors.");
            return 1;
        }

        Console.WriteLine("Party! 🥳 The Markdown linter does not detect any errors!");
        return 0;
    }
}
