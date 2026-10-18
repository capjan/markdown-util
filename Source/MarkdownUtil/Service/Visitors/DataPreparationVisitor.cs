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
        entity.HeadingTitle = header.Title;
        entity.FrontMatter = header.FrontMatter;
        var sidebarSettings = ReadFrontMatterSettings(header.FrontMatter);
        entity.Title = sidebarSettings.Title ?? header.Title;
        entity.Description = sidebarSettings.Description ?? string.Empty;
        entity.Icon = sidebarSettings.Icon ?? string.Empty;
        entity.SidebarTitle = sidebarSettings.SidebarTitle ?? string.Empty;
        entity.SidebarOrder = sidebarSettings.Order;
        entity.SidebarVisible = sidebarSettings.Visible;
        entity.SidebarExpanded = sidebarSettings.Expanded;
        return true;
    }

    private static FrontMatterSettings ReadFrontMatterSettings(string frontMatter)
    {
        if (string.IsNullOrWhiteSpace(frontMatter)) return new FrontMatterSettings();

        var lines = frontMatter.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[0].Trim() == "---") lines.RemoveAt(0);
        if (lines.Count > 0 && lines[^1].Trim() == "---") lines.RemoveAt(lines.Count - 1);

        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(string.Join(Environment.NewLine, lines)));

            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode metadata)
                return new FrontMatterSettings();

            var title = ReadScalar(metadata, "title");
            var description = ReadScalar(metadata, "description");
            var icon = ReadScalar(metadata, "icon");
            string? sidebarTitle = null;

            YamlNode? orderNode = null;
            YamlNode? visibleNode = null;
            YamlNode? expandedNode = null;
            if (metadata.Children.TryGetValue(new YamlScalarNode("sidebar"), out var sidebarNode) &&
                sidebarNode is YamlMappingNode sidebar)
            {
                sidebarTitle = ReadScalar(sidebar, "title");
                if (sidebar.Children.TryGetValue(new YamlScalarNode("order"), out var nestedOrderNode))
                    orderNode = nestedOrderNode;
                if (sidebar.Children.TryGetValue(new YamlScalarNode("visible"), out var nestedVisibleNode))
                    visibleNode = nestedVisibleNode;
                if (sidebar.Children.TryGetValue(new YamlScalarNode("expanded"), out var nestedExpandedNode))
                    expandedNode = nestedExpandedNode;
            }

            if (orderNode is null && metadata.Children.TryGetValue(new YamlScalarNode("sidebar.order"), out var compactOrderNode))
            {
                orderNode = compactOrderNode;
            }
            if (visibleNode is null && metadata.Children.TryGetValue(new YamlScalarNode("sidebar.visible"), out var compactVisibleNode))
            {
                visibleNode = compactVisibleNode;
            }
            if (expandedNode is null && metadata.Children.TryGetValue(new YamlScalarNode("sidebar.expanded"), out var compactExpandedNode))
            {
                expandedNode = compactExpandedNode;
            }

            int? order = null;
            if (orderNode is YamlScalarNode orderValue &&
                int.TryParse(orderValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedOrder))
            {
                order = parsedOrder;
            }

            var visible = visibleNode is YamlScalarNode visibleValue &&
                          bool.TryParse(visibleValue.Value, out var parsedVisible)
                ? parsedVisible
                : true;
            var expanded = expandedNode is YamlScalarNode expandedValue &&
                           bool.TryParse(expandedValue.Value, out var parsedExpanded) && parsedExpanded;

            return new FrontMatterSettings(title, description, icon, sidebarTitle, order, visible, expanded);
        }
        catch (YamlException)
        {
            return new FrontMatterSettings();
        }
    }

    private static string? ReadScalar(YamlMappingNode mapping, string key) =>
        mapping.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is YamlScalarNode scalar
            ? scalar.Value
            : null;

    private sealed record FrontMatterSettings(
        string? Title = null,
        string? Description = null,
        string? Icon = null,
        string? SidebarTitle = null,
        int? Order = null,
        bool Visible = true,
        bool Expanded = false);
}
