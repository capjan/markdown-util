using System.ComponentModel;
using Spectre.Console.Cli;

namespace MarkdownUtil.Commands.Settings;

public sealed class VisitorSettings : CommandSettings, IVisitorSettings
{
    
    
    [CommandOption("-p|--pattern <PATTERN>")]
    [DefaultValue(Default.SearchPattern)]
    public string? SearchPattern { get; init; }

    [CommandOption("--hidden")]
    [DefaultValue(true)]
    public bool IncludeHidden { get; init; }
    
    [Description("Root/Home Path for the Index")]
    [CommandArgument(0, "[PATH]")]
    public string? RootPath { get; init; }
}