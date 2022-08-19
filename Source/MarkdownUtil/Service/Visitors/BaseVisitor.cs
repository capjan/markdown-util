using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Service.Visitors;

public abstract class BaseVisitor : IVisitor<MarkdownFile>
{
    private readonly string _rootPath;
    private readonly string _outPath;
    private readonly string? _outputFileExtension;

    protected BaseVisitor(string rootPath, string outPath, string? outputFileExtension = default, GraphTraversalAlgorithm algorithm = GraphTraversalAlgorithm.Default)
    {
        _rootPath = rootPath;
        _outPath = outPath;
        _outputFileExtension = outputFileExtension;
        Algorithm = algorithm;
    }

    public GraphTraversalAlgorithm Algorithm { get; }
    
    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var pathInfo = FilePathInfo.CreateRenderFileInfo(_rootPath, _outPath, entity.FileInfo, _outputFileExtension);
        return ProcessFile(entity, graphDepth, pathInfo);
    }

    protected abstract bool ProcessFile(MarkdownFile entity, int graphDepth, FilePathInfo pathInfo);
}