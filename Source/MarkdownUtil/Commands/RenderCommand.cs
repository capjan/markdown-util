using System.Diagnostics.CodeAnalysis;
using Core.Collections.NodeGraph.Extensions;
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
    private readonly HtmlRenderer _renderer;

    public RenderCommand(GraphBuilder graphBuilder, CountNodesVisitor<MarkdownFile> countNodesVisitor, HtmlRenderer renderer)
    {
        _graphBuilder = graphBuilder;
        _countNodesVisitor = countNodesVisitor;
        _renderer = renderer;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] RenderCommandSettings settings)
    {
        var rootPath = settings.RootPath ?? throw new ArgumentNullException(nameof(settings.RootPath));
        var outPath = settings.OutputPath ?? throw new ArgumentNullException(nameof(settings.OutputPath));
        
        _renderer.PrepareBasePath(outPath);
        var graph = _graphBuilder.CreateGraph(settings);
        var nodeCount = CountMarkdownNodes(graph);
        
        AnsiConsole.WriteLine($"Preparing Assets");
        CopyAssets(rootPath, outPath, graph);
        
        AnsiConsole.WriteLine($"Rendering {nodeCount} Files");
        RenderNodesToHtml(rootPath, outPath, graph, settings.EditPageRoot);
        
        return 0;
    }

    private void RenderNodesToHtml(string rootPath, string outPath, MarkdownFile[] graph, string editPageRoot)
    {
        var rendererVisitor = new MarkdigVisitor(rootPath, outPath, _renderer, editPageRoot);
        
        graph.Visit(rendererVisitor);
    }

    private static void CopyAssets(string rootPath, string outPath, MarkdownFile[] graph)
    {
        var assetsVisitor = new LocalAssetsCopy(rootPath, outPath);
        graph.Visit(assetsVisitor);
    }

    private int CountMarkdownNodes(MarkdownFile[] graph)
    {
        graph.Visit(_countNodesVisitor);
        var nodeCount = _countNodesVisitor.NodeCount;
        return nodeCount;
    }
}