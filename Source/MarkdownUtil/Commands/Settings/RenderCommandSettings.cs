using System.ComponentModel;
using Spectre.Console.Cli;

namespace MarkdownUtil.Commands.Settings;

public sealed class RenderCommandSettings : CommandSettings, IVisitorSettings
{

    [CommandOption("-p|--pattern <PATTERN>")]
    [DefaultValue(Default.SearchPattern)]
    public string? SearchPattern { get; init; }

    [CommandOption("--hidden")]
    [DefaultValue(true)]
    public bool IncludeHidden { get; init; }

    [CommandOption("--editPageRoot")]
    [Description("URL to the root url for editing the source files")]
    [DefaultValue("")]
    public string EditPageRoot { get; set; } = string.Empty;
    
    [Description("Root/Home Path for the Index")]
    [CommandArgument(0, "[PATH]")]
    public string? RootPath { get; init; }
    
    [Description("Path to the output folder where the rendered files will be saved")]
    [CommandArgument(1, "[OUTPATH]")]
    public string? OutputPath { get; init; }
}