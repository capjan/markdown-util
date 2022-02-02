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
    public MarkdownFile? Parent { get; }
    public MarkdownFile[] Children { get; set; } = Array.Empty<MarkdownFile>();

    public string Title { get; set; } = string.Empty;
}
