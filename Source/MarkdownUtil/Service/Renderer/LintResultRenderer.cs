using Spectre.Console;

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
            AnsiConsole.WriteLine($"Linter detected [red]{errorCount} error[/].");
            return 1;
        }
        if (errorCount > 1)
        {
            AnsiConsole.MarkupLine($"Linter detected [red]{errorCount} errors[/].");
            return 1;
        }

        AnsiConsole.MarkupLine("Party! :partying_face: [green]The Markdown linter does not detect any errors![/]");
        return 0;
    }
}