using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class PrintTreeCommand
{
    private readonly GraphBuilder _graphBuilder;
    private readonly PrintTreeVisitor _printTreeVisitor;

    public PrintTreeCommand(GraphBuilder graphBuilder, PrintTreeVisitor printTreeVisitor)
    {
        _graphBuilder = graphBuilder;
        _printTreeVisitor = printTreeVisitor;
    }

    public int Execute(VisitorSettings settings)
    {
        var graph = _graphBuilder.CreateGraph(settings);
        graph.Visit(_printTreeVisitor);
        return 0;
    }
}
