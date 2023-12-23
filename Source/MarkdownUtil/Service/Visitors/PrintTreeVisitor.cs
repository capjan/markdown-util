using Core.Collections.NodeGraph;
using MarkdownUtil.Model;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class PrintTreeVisitor : IVisitor<MarkdownFile>
{
    public TraversalAlgorithm Algorithm => TraversalAlgorithm.DepthFirst;
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var indentString = new string(' ', graphDepth*2);
        AnsiConsole.WriteLine($"{indentString}{entity.Title}");
        return true;
    }
}
