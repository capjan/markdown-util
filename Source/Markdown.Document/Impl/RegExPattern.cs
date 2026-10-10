namespace Markdown.Document.Impl;

internal static class RegExPattern
{
    /// <summary>Matches front matter and the optional first-level heading.</summary>
    public const string MarkdownHeader =
        @"^(?<frontMatter>---[\s\S]*?---)?(?:[\s\n\r]*#\s+(?<titleName>.+))?";
}
