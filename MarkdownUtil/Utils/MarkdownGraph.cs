using MarkdownUtil.Model;
using MarkdownUtil.Utils.Graph;

namespace MarkdownUtil.Utils;

public class MarkdownGraph: NodeGraph<MarkdownFile>
{
    public MarkdownGraph(IEnumerable<MarkdownFile> rootContent)
    {
        RootContent = rootContent;
    }
}
