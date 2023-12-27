using System.Text;
using Core.Extensions.CollectionRelated;
using Core.Collections.NodeGraph;
using Markdown.Document.Impl;
using Markdown.Document.Model;
using Markdown.Document.Model.Impl;
using MarkdownUtil.Model;
using Spectre.Console;

namespace MarkdownUtil.Service.Visitors;

public class BreadcrumbsVisitor : IVisitor<MarkdownFile>
{
    public TraversalAlgorithm Algorithm => TraversalAlgorithm.DepthFirst;

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
      
        
        var crumbs = new List<string>();

        if (parents.Count != 0)
        {
            int upLevel = parents.Count;
            string lastParentCrumb;
            var lastParent = parents.Last();
            if (lastParent.FileInfo.DirectoryName == entity.FileInfo.DirectoryName)
            {
                upLevel--;
                var link = string.Concat("./", lastParent.FileInfo.Name);
                lastParentCrumb = $"[{lastParent.Title}]({link})";
            }
            else
            {
                var link = string.Concat("../", lastParent.FileInfo.Name);
                lastParentCrumb = $"[{lastParent.Title}]({link})";
            }


            for (var index = 0; index < parents.Count -1; index++)
            {
                var parent = parents[index];
                var link = string.Concat(Enumerable.Repeat("../", upLevel)) + parent.FileInfo.Name;
                crumbs.Add($"[{parent.Title}]({link})");
                upLevel--;
            }
            crumbs.Add(lastParentCrumb);
        } 
 
        crumbs.Add(entity.Title);
        var navigationLine = crumbs.ToSeparatedString(_settings.Separator);

        var header = new MarkdownHeader(entity.Title, navigationLine, entity.FrontMatter);
        _markdownDocumentWriter.WriteHeader(entity.FileInfo, header as IMarkdownHeader);
        AnsiConsole.WriteLine(navigationLine);
        return true;
    }
}

public class BreadcrumbSettings
{
    public string Separator { get; init; } = " \x2022 ";
};
