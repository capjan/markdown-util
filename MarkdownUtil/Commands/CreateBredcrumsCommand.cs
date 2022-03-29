using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MarkdownDocument.Impl;
using MarkdownUtil.Service;
using MarkdownUtil.Service.Visitors;
using Spectre.Console.Cli;
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace MarkdownUtil.Commands;

public class CreateBredcrumsCommand : Command<CreateBredcrumsCommand.Settings>
{

    private const string PatternDefaultValue = "README.md";
    public sealed class Settings : CommandSettings
    {
        [Description("Root/Home Path for the Index")]
        [CommandArgument(0, "[rootPath]")]
        public string? RootPath { get; init; }

        [CommandOption("-p|--pattern")]
        [DefaultValue(PatternDefaultValue)]
        public string? SearchPattern { get; init; }

        [CommandOption("--hidden")]
        [DefaultValue(true)]
        public bool IncludeHidden { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var searchOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System,
            RecurseSubdirectories = false
        };

        var rootPath = settings.RootPath ?? Directory.GetCurrentDirectory();
        var searchPattern = settings.SearchPattern ?? PatternDefaultValue;
        var markdownReader = new MarkdownDocumentReader();

        var rootDirectory = new DirectoryInfo(rootPath);

        var lintErrorReceiver = new ConsoleLintErrorReceiver();
        var service = new MarkdownService();
        var graph = service.CreateGraph(rootDirectory, searchPattern, searchOptions);

        graph.Visit(new DataPreparationVisitor(markdownReader));

        var lintVisitor = new LinterVisitor(lintErrorReceiver);
        graph.Visit(lintVisitor);
        if (lintErrorReceiver.ErrorCount != 0)
            return 1;
        graph.Visit(new BreadcrumbsVisitor());
        graph.Visit(new PrintTreeVisitor());

        return 0;
    }


}
