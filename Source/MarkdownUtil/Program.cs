// See https://aka.ms/new-console-template for more information

using Markdown.Document;
using Markdown.Document.Impl;
using Markdown.Renderer;
using MarkdownUtil.Commands;
using MarkdownUtil.Commands.Settings;
using MarkdownUtil.Model;
using MarkdownUtil.Service;
using MarkdownUtil.Service.DI;
using MarkdownUtil.Service.Renderer;
using MarkdownUtil.Service.Visitors;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

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
registrations.AddScoped<RenderCommandSettings>();
registrations.AddScoped<VisitorSettings>();

var registrar = new TypeRegistrar(registrations);


var app = new CommandApp(registrar);

app.Configure(config =>
{
    
    config.Settings.ApplicationName = "mdu";
    config.AddCommand<AddBreadcrumbNavigationCommand>("add-breadcrumb-navigation")
        .WithAlias("abn")
        .WithDescription("adds breadcrumb navigation to all matching markdown files.\nAlias: abn");
    config.AddCommand<LintCommand>("lint")
        .WithAlias("l")
        .WithDescription("Runs the Linter and prints the results");
    config.AddCommand<PrintTreeCommand>("print-tree")
        .WithAlias("print")
        .WithAlias("tree")
        .WithDescription("Prints the detected markdown files as tree to stdout");
    config.AddCommand<RenderCommand>("render")
        .WithAlias("r")
        .WithDescription("Renders all Markdown files to the given output path");
});
return app.Run(args);
