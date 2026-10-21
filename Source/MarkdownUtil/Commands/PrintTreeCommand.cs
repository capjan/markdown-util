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

        MarkdownUtil.Model.MarkdownFile[] graph;
        try
        {
            graph = _graphBuilder.CreateGraph(new VisitorSettings
            {
                RootPath = rootPath,
                SearchPattern = settings.SearchPattern,
                IncludeHidden = settings.IncludeHidden
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"Could not resolve Markdown sources: {exception.Message}");
            return 1;
        }
        graph.Visit(_printTreeVisitor);
        return 0;
    }
}
