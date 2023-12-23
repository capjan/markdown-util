using System.Diagnostics.CodeAnalysis;
using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;
using Spectre.Console.Cli;
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

namespace MarkdownUtil.Commands;

public class PrintTreeCommand : Command<VisitorSettings>
{
    private readonly GraphBuilder _graphBuilder;
    private readonly PrintTreeVisitor _printTreeVisitor;

    public PrintTreeCommand(GraphBuilder graphBuilder, PrintTreeVisitor printTreeVisitor)
    {
        _graphBuilder = graphBuilder;
        _printTreeVisitor = printTreeVisitor;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] VisitorSettings settings)
    {
        var graph = _graphBuilder.CreateGraph(settings);
        graph.Visit(_printTreeVisitor);
        return 0;
    }
}