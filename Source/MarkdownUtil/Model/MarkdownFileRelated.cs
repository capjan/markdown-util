namespace MarkdownUtil.Model;

public static class MarkdownFileRelated
{
    public static IReadOnlyList<MarkdownFile> Parents(this MarkdownFile value)
    {
        if (value.Parent == null) return Array.Empty<MarkdownFile>();

        var parents = new List<MarkdownFile>();
        var parent = value.Parent;

        while (parent != null)
        {
            parents.Add(parent);
            parent = parent.Parent;
        }

        parents.Reverse();
        return parents;
    }
}
