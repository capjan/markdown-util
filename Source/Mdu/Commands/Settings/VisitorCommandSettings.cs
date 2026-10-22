namespace Mdu.Commands.Settings;

public sealed class VisitorSettings : IVisitorSettings
{
    public string? SearchPattern { get; init; }

    public bool IncludeHidden { get; init; }

    public string? RootPath { get; init; }
}
