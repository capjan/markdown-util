// See https://aka.ms/new-console-template for more information

using MarkdownUtil.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<CreateBredcrumsCommand>();
return app.Run(args);
