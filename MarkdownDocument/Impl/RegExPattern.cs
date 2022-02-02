namespace MarkdownDocument.Impl;

internal static class RegExPattern
{
    /// <summary>
    /// Matches any Markdown Header. Groups the Optional Title and Breadcrumbs Navigation line
    /// </summary>
    public const string MarkdownHeader =
        @"^(?:#\s+(?<titleName>[\w\s-[\n]]+)(?=\r?\n))?(?:\s*(?:\r?\n))*(?<breadcrumbs>\[.*)?(?:\s*(?:\r?\n))*(?=.)";
}
