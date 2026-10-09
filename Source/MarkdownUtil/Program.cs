using Markdown.Document;
using Markdown.Document.Impl;
using Markdown.Renderer;
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
registrations.AddScoped<BreadcrumbsVisitor>();
registrations.AddScoped<PrintTreeVisitor>();
registrations.AddScoped<LintResultRenderer>();
registrations.AddScoped<GraphBuilder>();
registrations.AddScoped<CountNodesVisitor<MarkdownFile>>();
registrations.AddScoped<HtmlRenderer>();
registrations.AddScoped<AddBreadcrumbNavigationCommand>();
registrations.AddScoped<LintCommand>();
registrations.AddScoped<PrintTreeCommand>();
registrations.AddScoped<RenderCommand>();

using var services = registrations.BuildServiceProvider();

var rootCommand = new RootCommand("Lint, render, and manage Markdown documentation.");

var addBreadcrumbNavigationCommand = new Command(
    "add-breadcrumb-navigation",
    "Adds breadcrumb navigation to all matching Markdown files.");
addBreadcrumbNavigationCommand.Aliases.Add("abn");
var (breadcrumbPattern, breadcrumbHidden) = AddVisitorOptions(addBreadcrumbNavigationCommand);
var breadcrumbPath = AddRootPathArgument(addBreadcrumbNavigationCommand);
addBreadcrumbNavigationCommand.SetAction(parseResult =>
    services.GetRequiredService<AddBreadcrumbNavigationCommand>().Execute(
        GetVisitorSettings(parseResult, breadcrumbPath, breadcrumbPattern, breadcrumbHidden)));
rootCommand.Subcommands.Add(addBreadcrumbNavigationCommand);

var lintCommand = new Command("lint", "Runs the linter and prints the results.");
lintCommand.Aliases.Add("l");
var (lintPattern, lintHidden) = AddVisitorOptions(lintCommand);
var lintPath = AddRootPathArgument(lintCommand);
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

var renderCommand = new Command("render", "Renders all Markdown files to the given output path.");
renderCommand.Aliases.Add("r");
var (renderPattern, renderHidden) = AddVisitorOptions(renderCommand);
var renderRootPath = new Argument<string?>("path")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Root/home path for the index."
};
var outputPath = new Argument<string?>("outpath")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Path to the output folder for rendered files."
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

static Argument<string?> AddRootPathArgument(Command command)
{
    var path = new Argument<string?>("path")
    {
        Arity = ArgumentArity.ZeroOrOne,
        Description = "Root/home path for the index."
    };
    command.Arguments.Add(path);
    return path;
}
