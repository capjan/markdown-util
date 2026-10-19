
using Core.Collections.NodeGraph;

namespace MarkdownUtil.Model;

public class MarkdownFile: INode<MarkdownFile>
{
    public MarkdownFile(FileInfo fileInfo, MarkdownFile? parent)
    {
        FileInfo = fileInfo;
        Parent = parent;
    }

    public FileInfo FileInfo { get; }
    public MarkdownFile? Parent { get; set; }
    public MarkdownFile[] Children { get; set; } = Array.Empty<MarkdownFile>();

    public string Title { get; set; } = string.Empty;
    public string HeadingTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string FrontMatter { get; set; } = string.Empty;
    public int? NavigationOrder { get; set; }
    public string NavigationTitle { get; set; } = string.Empty;
    public bool NavigationVisible { get; set; } = true;
    public bool? NavigationExpanded { get; set; }
    public NavigationPlacement NavigationPlacement { get; set; }
    public string? InvalidNavigationPlacement { get; set; }
    public bool IsOnlyReadmeInSection { get; set; }
}
