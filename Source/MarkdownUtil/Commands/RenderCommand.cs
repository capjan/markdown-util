using Core.Collections.NodeGraph.Extensions;
using Markdown.Renderer;
using Markdown.Search;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class RenderCommand
{
    private readonly GraphBuilder _graphBuilder;
    private readonly CountNodesVisitor<MarkdownFile> _countNodesVisitor;
    private readonly HtmlRenderer _renderer;
    private readonly SearchIndexBuilder _searchIndexBuilder;

    public RenderCommand(
        GraphBuilder graphBuilder,
        CountNodesVisitor<MarkdownFile> countNodesVisitor,
        HtmlRenderer renderer,
        SearchIndexBuilder searchIndexBuilder)
    {
        _graphBuilder = graphBuilder;
        _countNodesVisitor = countNodesVisitor;
        _renderer = renderer;
        _searchIndexBuilder = searchIndexBuilder;
    }

    public int Execute(RenderCommandSettings settings)
    {
        string rootPath;
        try
        {
            rootPath = CommandPathDefaults.ResolveRootPath(settings.RootPath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }

        var outPath = CommandPathDefaults.ResolveOutputPath(settings.OutputPath);
        var resolvedSettings = new RenderCommandSettings
        {
            RootPath = rootPath,
            OutputPath = outPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden,
            EditPageRoot = settings.EditPageRoot
        };
        
        var siteConfiguration = SiteConfigurationReader.Read(rootPath);
        _renderer.PrepareBasePath(outPath, siteConfiguration);
        SearchClientAsset.WriteTo(outPath);
        _searchIndexBuilder.Clear();
        var graph = _graphBuilder.CreateGraph(resolvedSettings);
        var nodeCount = CountMarkdownNodes(graph);
        
        Console.WriteLine("Preparing Assets");
        CopyAssets(rootPath, outPath, graph);
        SiteConfigurationReader.CopyAssets(rootPath, outPath, siteConfiguration);
        
        Console.WriteLine($"Rendering {nodeCount} Files");
        RenderNodesToHtml(rootPath, outPath, graph, settings.EditPageRoot);
        _searchIndexBuilder.WriteIndex(outPath);
        
        return 0;
    }

    private void RenderNodesToHtml(
        string rootPath,
        string outPath,
        MarkdownFile[] graph,
        string editPageRoot)
    {
        var rendererVisitor = new MarkdigVisitor(rootPath, outPath, _renderer, editPageRoot, _searchIndexBuilder, graph);
        
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
