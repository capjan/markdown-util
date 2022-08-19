using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class PrintTreeVisitor : IVisitor<MarkdownFile>
{
    public GraphTraversalAlgorithm Algorithm => GraphTraversalAlgorithm.DepthFirst;
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var indentString = new string(' ', graphDepth*2);
        AnsiConsole.WriteLine($"{indentString}{entity.Title}");
        return true;
    }
}
