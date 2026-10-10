using Markdown.Document;
using Markdown.Document.Impl;
using Markdown.Renderer;
using Markdown.Search;
using MarkdownUtil.Commands;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Renderer;
using MarkdownUtil.Service.Visitors;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

var registrations = new ServiceCollection();
registrations.AddScoped<ILinterErrorReceiver, ConsoleLintErrorReceiver>();
registrations.AddScoped<LinterVisitor>();
registrations.AddScoped<IMarkdownDocumentReader, MarkdownDocumentReader>();
registrations.AddScoped<DataPreparationVisitor>();
registrations.AddScoped<PrintTreeVisitor>();
registrations.AddScoped<LintResultRenderer>();
registrations.AddScoped<GraphBuilder>();
registrations.AddScoped<CountNodesVisitor<MarkdownFile>>();
registrations.AddScoped<HtmlRenderer>();
registrations.AddScoped<SearchIndexBuilder>();
registrations.AddScoped<LintCommand>();
registrations.AddScoped<PrintTreeCommand>();
registrations.AddScoped<RenderCommand>();
registrations.AddScoped<ServeCommand>();

using var services = registrations.BuildServiceProvider();

var rootCommand = new RootCommand("Lint, render, and manage Markdown documentation.");

var lintCommand = new Command("lint", "Runs the linter and prints the results.");
lintCommand.Aliases.Add("l");
var (lintPattern, lintHidden) = AddVisitorOptions(lintCommand);
var lintPath = AddRootPathArgument(lintCommand, "Root folder containing Markdown files. Defaults to ./docs or ./Docs.");
lintCommand.SetAction(parseResult =>
    services.GetRequiredService<LintCommand>().Execute(
        GetVisitorSettings(parseResult, lintPath, lintPattern, lintHidden)));
rootCommand.Subcommands.Add(lintCommand);

var printTreeCommand = new Command("print-tree", "Prints the detected Markdown files as a tree.");
printTreeCommand.Aliases.Add("print");
printTreeCommand.Aliases.Add("tree");
var (treePattern, treeHidden) = AddVisitorOptions(printTreeCommand);
var treePath = AddRootPathArgument(printTreeCommand);
printTreeCommand.SetAction(parseResult =>
    services.GetRequiredService<PrintTreeCommand>().Execute(
        GetVisitorSettings(parseResult, treePath, treePattern, treeHidden)));
rootCommand.Subcommands.Add(printTreeCommand);

var renderCommand = new Command("render", "Renders Markdown files to a static HTML site.");
renderCommand.Aliases.Add("r");
var (renderPattern, renderHidden) = AddVisitorOptions(renderCommand);
var renderRootPath = new Argument<string?>("path")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Root folder containing Markdown files. Defaults to ./docs or ./Docs."
};
var outputPath = new Argument<string?>("outpath")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Output folder for rendered files. Defaults to ./site."
};
var editPageRoot = new Option<string>("--editPageRoot")
{
    Description = "URL to the root URL for editing the source files.",
    DefaultValueFactory = _ => string.Empty
};
renderCommand.Arguments.Add(renderRootPath);
renderCommand.Arguments.Add(outputPath);
renderCommand.Options.Add(editPageRoot);
renderCommand.SetAction(parseResult =>
    services.GetRequiredService<RenderCommand>().Execute(new RenderCommandSettings
    {
        RootPath = parseResult.GetValue(renderRootPath),
        OutputPath = parseResult.GetValue(outputPath),
        SearchPattern = parseResult.GetValue(renderPattern),
        IncludeHidden = parseResult.GetValue(renderHidden),
        EditPageRoot = parseResult.GetValue(editPageRoot) ?? string.Empty
    }));
rootCommand.Subcommands.Add(renderCommand);

var serveCommand = new Command("serve", "Renders, serves, and watches a Markdown site.");
var (servePattern, serveHidden) = AddVisitorOptions(serveCommand);
var serveRootPath = new Argument<string?>("path")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Root folder containing the Markdown files. Defaults to ./docs or ./Docs."
};
var serveOutputPath = new Argument<string?>("outpath")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Output folder for the rendered site. Defaults to ./site."
};
var servePort = new Option<int>("--port")
{
    Description = "Local preview server port.",
    DefaultValueFactory = _ => 4173
};
var noBrowser = new Option<bool>("--no-browser")
{
    Description = "Do not open the preview in a browser."
};
var serveEditPageRoot = new Option<string>("--editPageRoot")
{
    Description = "URL to the root URL for editing the source files.",
    DefaultValueFactory = _ => string.Empty
};
serveCommand.Arguments.Add(serveRootPath);
serveCommand.Arguments.Add(serveOutputPath);
serveCommand.Options.Add(servePort);
serveCommand.Options.Add(noBrowser);
serveCommand.Options.Add(serveEditPageRoot);
serveCommand.SetAction(parseResult =>
    services.GetRequiredService<ServeCommand>().Execute(
        new RenderCommandSettings
        {
            RootPath = parseResult.GetValue(serveRootPath),
            OutputPath = parseResult.GetValue(serveOutputPath),
            SearchPattern = parseResult.GetValue(servePattern),
            IncludeHidden = parseResult.GetValue(serveHidden),
            EditPageRoot = parseResult.GetValue(serveEditPageRoot) ?? string.Empty
        },
        parseResult.GetValue(servePort),
        parseResult.GetValue(noBrowser)));
rootCommand.Subcommands.Add(serveCommand);

return rootCommand.Parse(args).Invoke();

static (Option<string?> Pattern, Option<bool> IncludeHidden) AddVisitorOptions(Command command)
{
    var pattern = new Option<string?>("--pattern", "-p")
    {
        Description = "Search pattern for Markdown files.",
        DefaultValueFactory = _ => Default.SearchPattern
    };
    var includeHidden = new Option<bool>("--hidden")
    {
        Description = "Include hidden files and folders.",
        DefaultValueFactory = _ => true
    };

    command.Options.Add(pattern);
    command.Options.Add(includeHidden);
    return (pattern, includeHidden);
}

static VisitorSettings GetVisitorSettings(
    ParseResult parseResult,
    Argument<string?> path,
    Option<string?> pattern,
    Option<bool> includeHidden) => new()
{
    RootPath = parseResult.GetValue(path),
    SearchPattern = parseResult.GetValue(pattern),
    IncludeHidden = parseResult.GetValue(includeHidden)
};

static Argument<string?> AddRootPathArgument(Command command, string description = "Root/home path for the index.")
{
    var path = new Argument<string?>("path")
    {
        Arity = ArgumentArity.ZeroOrOne,
        Description = description
    };
    command.Arguments.Add(path);
    return path;
}
