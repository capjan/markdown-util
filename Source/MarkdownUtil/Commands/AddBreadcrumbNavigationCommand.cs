using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class AddBreadcrumbNavigationCommand
{
    private readonly LinterVisitor _linterVisitor;
    private readonly BreadcrumbsVisitor _breadcrumbsVisitor;
    private readonly PrintTreeVisitor _printTreeVisitor;
    private readonly GraphBuilder _graphBuilder;
    
    public AddBreadcrumbNavigationCommand(
        GraphBuilder graphBuilder,
        LinterVisitor linterVisitor,
        BreadcrumbsVisitor breadcrumbsVisitor,
        PrintTreeVisitor printTreeVisitor)
    {
        _graphBuilder = graphBuilder;
        _linterVisitor = linterVisitor;
        _breadcrumbsVisitor = breadcrumbsVisitor;
        _printTreeVisitor = printTreeVisitor;
    }

    public int Execute(VisitorSettings settings)
    {
        var graph = _graphBuilder.CreateGraph(settings);

        graph.Visit(_linterVisitor);
        
        if (_linterVisitor.ErrorReceiver.ErrorCount != 0) return 1;
        graph.Visit(_breadcrumbsVisitor);
        graph.Visit(_printTreeVisitor);

        return 0;
    }


}
