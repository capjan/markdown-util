namespace MarkdownUtil.Commands.Settings;

public interface IVisitorSettings
{
    string? SearchPattern { get; init; }
    bool IncludeHidden { get; init; }
    string? RootPath { get; init; }
}