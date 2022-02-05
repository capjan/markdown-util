using System.Text;
using Core.Extensions.CollectionRelated;
using MarkdownDocument.Impl;
using MarkdownDocument.Model;
using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class BreadcrumbsVisitor : IVisitor<MarkdownFile>
{
    public GraphTraversalAlgorithm Algorithm => GraphTraversalAlgorithm.DepthFirst;

    private readonly BreadcrumbSettings _settings;
    private readonly MarkdownDocumentWriter _markdownDocumentWriter;

    public BreadcrumbsVisitor(MarkdownDocumentWriter? markdownDocumentWriter = default, BreadcrumbSettings? settings = default)
    {
        _markdownDocumentWriter = markdownDocumentWriter ?? new MarkdownDocumentWriter();
        _settings = settings ?? new BreadcrumbSettings();
    }

    public bool Process(MarkdownFile entity, int graphDepth)
    {
        var parents = entity.Parents();
        var crumbs = parents.Select((parent, index) =>
            {
                var distance = parents.Count - index;
                var link = string.Concat(Enumerable.Repeat("../", distance)) + parent.FileInfo.Name;
                return $"[{parent.Title}]({link})";
            })
            .ToList()
            .Append(entity.Title)
            .ToSeparatedString(_settings.Separator);

        var header = new MarkdownHeader(entity.Title, crumbs);
        _markdownDocumentWriter.WriteHeader(entity.FileInfo, header);
        AnsiConsole.WriteLine(crumbs);
        return true;
    }
}

public class BreadcrumbSettings
{
    public string Separator { get; init; } = " \x2022 ";
};
