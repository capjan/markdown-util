using MarkdownUtil.Utils.Graph;

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
    public string FrontMatter { get; set; } = string.Empty;
}
