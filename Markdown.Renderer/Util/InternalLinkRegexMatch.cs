namespace Markdown.Renderer.Util;

public struct InternalLinkRegexMatch
{
    public static InternalLinkRegexMatch NoRegexMatch = new InternalLinkRegexMatch
    {
        IsSuccess = false,
        Info = new MarkdownLinkInfo
        {
            FullMatch = string.Empty,
            LinkName = string.Empty,
            FileName = string.Empty,
            FileNameWithoutExtension = string.Empty,
            Extension = string.Empty
        }
    };

    public static InternalLinkRegexMatch Success(MarkdownLinkInfo info)
    {
        return new InternalLinkRegexMatch
        {
            IsSuccess = true,
            Info = info
        };
    }
    
    public bool IsSuccess { get; init; }
    public MarkdownLinkInfo Info { get; init; }
}