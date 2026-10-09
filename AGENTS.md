# Repository guide for agents

## Project purpose

MarkdownUtil is a .NET command-line tool distributed as a .NET tool with the `mdu` command. It helps documentation authors lint Markdown, inspect document structure, add breadcrumb navigation, and render Markdown files to static HTML. Source documents remain ordinary Markdown files.

## Architecture

- `Source/MarkdownUtil` contains the command-line entry point, command handlers, dependency injection setup, and orchestration services. `Program.cs` defines the CLI with `System.CommandLine`.
- `Source/Markdown.Document` reads Markdown files and exposes document, header, and link models. It uses Markdig for Markdown parsing.
- `Source/Markdown.Linter` contains validation rules and depends on `Source/SuperiorIO` for path and file helpers.
- `Source/Markdown.Renderer` renders documents to HTML with RazorLight and Markdig. `Source/StaticCode.Highlighter` provides syntax highlighting for rendered code blocks.
- `Source/Core.Collections.NodeGraph` supplies the graph and visitor abstractions used to traverse Markdown documents.
- Projects ending in `.Test` contain the corresponding xUnit tests.
- `Docs` contains the user documentation. `.github/workflows` contains CI and NuGet publishing workflows.

## Repository goals and constraints

- Keep Markdown files portable and avoid tying user content to a hosting vendor.
- Preserve existing CLI command names, aliases, arguments, and behavior when changing the CLI.
- Keep changes focused on the requested behavior; avoid unrelated refactors and speculative abstractions.
- All projects target `net10.0`. Use standard `dotnet` commands; this repository no longer uses NUKE.
- `Source/MarkdownUtil/MarkdownUtil.csproj` defines the packaged tool and its `mdu` command name.

## Working in this repository

- Restore and build with `dotnet restore MarkdownUtils.sln` and `dotnet build MarkdownUtils.sln`.
- Run the test suite with `dotnet test MarkdownUtils.sln` when tests are requested or needed to verify a change.
- Pack the CLI tool with `dotnet pack Source/MarkdownUtil/MarkdownUtil.csproj --configuration Release`.
- Run `dotnet doctor fix` to install this repository's local Git hooks. Use `dotnet doctor diagnose all` to check the hook setup; commit messages must follow Conventional Commits.
- Keep CLI output plain-text and dependency-light. `System.CommandLine` is the command parser; do not reintroduce Spectre.Console.
