using System.Globalization;
using System.Text;
using Core.Parser.Special;
using Core.Collections.NodeGraph;
using Markdown.Document;
using MarkdownUtil.Model;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace MarkdownUtil.Service.Visitors;

public class DataPreparationVisitor: IVisitor<MarkdownFile>
{
    public TraversalAlgorithm Algorithm => TraversalAlgorithm.DepthFirst;

    private readonly IMarkdownDocumentReader _markdownReader;

    public DataPreparationVisitor(IMarkdownDocumentReader markdownReader)
    {
        _markdownReader = markdownReader;
    }

    public bool Process(MarkdownFile entity, int graphDepth)
    {
        using var reader = new StreamReader(entity.FileInfo.FullName, Encoding.UTF8);
        var input = new ParserInput(reader);
        var header = _markdownReader.ReadHeader(input);
        entity.Title = header.Title;
        entity.FrontMatter = header.FrontMatter;
        entity.SidebarOrder = ReadSidebarOrder(header.FrontMatter);
        return true;
    }

    private static int? ReadSidebarOrder(string frontMatter)
    {
        if (string.IsNullOrWhiteSpace(frontMatter) || !frontMatter.Contains("sidebar", StringComparison.Ordinal))
            return null;

        var lines = frontMatter.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[0].Trim() == "---") lines.RemoveAt(0);
        if (lines.Count > 0 && lines[^1].Trim() == "---") lines.RemoveAt(lines.Count - 1);

        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(string.Join(Environment.NewLine, lines)));

            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode metadata)
                return null;

            YamlNode? orderNode = null;
            if (metadata.Children.TryGetValue(new YamlScalarNode("sidebar"), out var sidebarNode) &&
                sidebarNode is YamlMappingNode sidebar &&
                sidebar.Children.TryGetValue(new YamlScalarNode("order"), out var nestedOrderNode))
            {
                orderNode = nestedOrderNode;
            }
            else if (metadata.Children.TryGetValue(new YamlScalarNode("sidebar.order"), out var compactOrderNode))
            {
                orderNode = compactOrderNode;
            }

            if (orderNode is not YamlScalarNode orderValue)
                return null;

            return int.TryParse(orderValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order)
                ? order
                : null;
        }
        catch (YamlException)
        {
            return null;
        }
    }
}
