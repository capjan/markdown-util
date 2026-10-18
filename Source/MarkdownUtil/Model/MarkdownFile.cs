
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
    public int? SidebarOrder { get; set; }
    public string SidebarTitle { get; set; } = string.Empty;
    public bool SidebarVisible { get; set; } = true;
    public bool SidebarExpanded { get; set; }
    public bool IsOnlyReadmeInSection { get; set; }
}
