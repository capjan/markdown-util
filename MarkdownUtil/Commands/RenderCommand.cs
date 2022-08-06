using System.Diagnostics.CodeAnalysis;
using Markdown.Renderer;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;
using Spectre.Console;
using Spectre.Console.Cli;
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

namespace MarkdownUtil.Commands;

public class RenderCommand : Command<RenderCommandSettings>
{
    private readonly GraphBuilder _graphBuilder;
    private readonly CountNodesVisitor<MarkdownFile> _countNodesVisitor;
    private readonly HtmlRenderer _htmlFoundation;

    public RenderCommand(GraphBuilder graphBuilder, CountNodesVisitor<MarkdownFile> countNodesVisitor, HtmlRenderer htmlFoundation)
    {
        _graphBuilder = graphBuilder;
        _countNodesVisitor = countNodesVisitor;
        _htmlFoundation = htmlFoundation;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] RenderCommandSettings settings)
    {
        var rootPath = settings.RootPath ?? throw new ArgumentNullException(nameof(settings.RootPath));
        var outPath = settings.OutputPath ?? throw new ArgumentNullException(nameof(settings.OutputPath));
        _htmlFoundation.PrepareBasePath(outPath);
        var graph = _graphBuilder.CreateGraph(settings);
        graph.Visit(_countNodesVisitor);
        var nodeCount = _countNodesVisitor.NodeCount;
        AnsiConsole.WriteLine($"Preparing Assets");
        var assetsVisitor = new LocalAssetsCopy(rootPath, outPath);
        graph.Visit(assetsVisitor);
        AnsiConsole.WriteLine($"Rendering {nodeCount} Files");
        var rendererVisitor = new MarkdigVisitor(rootPath, outPath, _htmlFoundation);
        graph.Visit(rendererVisitor);
        return 0;
    }
}