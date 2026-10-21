using Core.Collections.NodeGraph.Extensions;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Renderer;
using MarkdownUtil.Service.Visitors;

namespace MarkdownUtil.Commands;

public class LintCommand
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

    public int Execute(VisitorSettings settings, bool pretty = false, bool color = false, LintFormat format = LintFormat.Text)
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

        // JSON output must stay a single document.
        if (format != LintFormat.Json) Console.WriteLine("Running Linter");
        var graph = _graphBuilder.CreateGraph(new VisitorSettings
        {
            RootPath = rootPath,
            SearchPattern = settings.SearchPattern,
            IncludeHidden = settings.IncludeHidden
        });
        _linterVisitor.Prepare(graph, rootPath);
        graph.Visit(_linterVisitor);
        return format == LintFormat.Text
            ? _lintResultRenderer.Render(_linterVisitor.OrphanedPages, rootPath, pretty, color)
            : _lintResultRenderer.Render(format, _linterVisitor.OrphanedPages, rootPath);
    }
}
