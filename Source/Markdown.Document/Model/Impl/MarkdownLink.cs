using Markdown.Document.Model;
using Markdown.Document.Model.Impl;


namespace Markdown.Document.Model.Impl;

public static class Contants
{
    public static IMarkdownLink EmptyLink = new MarkdownLink("", "", false, 0);    
}


public record MarkdownLink(string Name, string Destination, bool IsImageLink, int LineNumber) : IMarkdownLink;

