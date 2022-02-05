namespace MarkdownDocument.Impl;

internal static class RegExPattern
{
    /// <summary>
    /// Matches any Markdown Header. Groups the Optional Title and Breadcrumbs Navigation line
    /// </summary>
    public const string MarkdownHeader =
        @"^(?:[\s\n\r]*#\s+(?<titleName>.+))?(?:\s*(?:\r?\n))*(?<breadcrumbs>(?:\[.*|\k<titleName>))?(?:\s*(?:\r?\n))*(?=.)?";
}
