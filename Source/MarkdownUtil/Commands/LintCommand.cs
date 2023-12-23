using System.Diagnostics.CodeAnalysis;
using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Renderer;
using MarkdownUtil.Service.Visitors;
using Spectre.Console;
using Spectre.Console.Cli;
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

namespace MarkdownUtil.Commands;

public class LintCommand : Command<VisitorSettings>
{
    private readonly GraphBuilder _graphBuilder;
    private readonly LinterVisitor _linterVisitor;
    private readonly LintResultRenderer _lintResultRenderer;

    public LintCommand(GraphBuilder graphBuilder, LinterVisitor linterVisitor, LintResultRenderer lintResultRenderer)
    {
        _linterVisitor = linterVisitor;
        _lintResultRenderer = lintResultRenderer;
        _graphBuilder = graphBuilder;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] VisitorSettings settings)

    {
        AnsiConsole.MarkupLine("Running [green]Linter[/]");
        var graph = _graphBuilder.CreateGraph(settings);
        graph.Visit(_linterVisitor);
        return _lintResultRenderer.Render();
    }
}