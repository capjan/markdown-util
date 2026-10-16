# Repository guide for agents

## Project purpose

MarkdownUtil is a .NET command-line tool distributed as a .NET tool with the `mdu` command. It helps documentation authors lint Markdown, inspect document structure, and render Markdown files to static HTML with breadcrumb navigation generated from the document structure. Source documents remain ordinary Markdown files.

## Architecture

- `Source/MarkdownUtil` contains the command-line entry point, command handlers, dependency injection setup, and orchestration services. `Program.cs` defines the CLI with `System.CommandLine`.
- `Source/Markdown.Document` reads Markdown files and exposes document, header, and link models. It uses Markdig for Markdown parsing.
- `Source/Markdown.Linter` contains validation rules and depends on `Source/SuperiorIO` for path and file helpers.
- `Source/Markdown.Renderer` renders documents to HTML with an embedded template, .NET HTML encoding, and Markdig. `Source/StaticCode.Highlighter` provides syntax highlighting for rendered code blocks. `Source/StaticCode.Diagrams` renders a Mermaid subset to static SVG: flowcharts with its own layered (Sugiyama) layout and sequence diagrams, PlantUML component diagrams in `plantuml` code blocks with containers laid out from the inside out, and its own radial context diagrams in `context` code blocks; the SVG carries only CSS classes, so `main.css` themes it for light and dark mode. `Source/StaticCode.Icons` embeds the icon sets behind the Material for MkDocs–compatible icon shortcodes (`:material-…:`, `:fontawesome-…:`, `:octicons-…:`, `:simple-…:`) and the colored Dashboard Icons (`:dashboard-…:`, sanitized with SVGO by `dashboard-icons.mjs`) as gzip-compressed, reduced Iconify JSON; regenerate them with `Source/StaticCode.Icons/update-icon-sets.sh` (needs npm) instead of editing `Res/*.json.gz`. Emoji shortcodes use Markdig's GitHub emoji mapping and render as Unicode. Keyboard keys use the pymdownx Keys syntax of Material for MkDocs (`++ctrl+alt+del++`); the key map with its symbols is in `Source/Markdown.Renderer/MarkdigExtension/KeyboardKeys.cs`.
- `Source/Core.Collections.NodeGraph` supplies the graph and visitor abstractions used to traverse Markdown documents.
- Projects ending in `.Test` contain the corresponding xUnit tests.
- `Docs` contains the user documentation. `.github/workflows` contains CI, Pages deployment, and release workflows.

## Repository goals and constraints

- Keep Markdown files portable and avoid tying user content to a hosting vendor.
- Preserve existing CLI command names, aliases, arguments, and behavior when changing the CLI.
- Keep changes focused on the requested behavior; avoid unrelated refactors and speculative abstractions.
- All projects target `net10.0`. Use standard `dotnet` commands; this repository no longer uses NUKE.
- `Source/MarkdownUtil/MarkdownUtil.csproj` defines the packaged tool and its `mdu` command name. The package version is supplied at release time from git-cliff; do not add a second hard-coded package version.
- `cliff.toml` defines Conventional Commit changelog groups and version bumps: `feat` bumps minor, `fix` and `perf` bump patch, breaking changes (including `!` or `BREAKING CHANGE` on otherwise ignored types) bump major, and maintenance-only changes do not bump.
- Start releases manually from the `main` branch with the `Release` workflow. It uses git-cliff for the GitHub release tag, notes, and the `dotnet pack` `Version` property, then publishes through NuGet Trusted Publishing. NuGet's trusted publisher is configured for `.github/workflows/release.yml` with no GitHub Environment; `NUGET_USER` is the NuGet profile name stored as a GitHub Actions secret.

## Working in this repository

- Restore and build with `dotnet restore MarkdownUtils.sln` and `dotnet build MarkdownUtils.sln`.
- Run the test suite with `dotnet test MarkdownUtils.sln` when tests are requested or needed to verify a change.
- Pack the CLI tool for a release with `dotnet pack Source/MarkdownUtil/MarkdownUtil.csproj --configuration Release -p:Version="$PACKAGE_VERSION"`, where `PACKAGE_VERSION` is the git-cliff version without the leading `v`.
- To update the globally installed `mdu` with a local build for manual testing, pack it with a unique local version and install from a temporary NuGet source:
  ```sh
  PACKAGE_VERSION=3.0.0-local.YYYYMMDD.N
  PACKAGE_DIR=/tmp/mdu-local-package
  mkdir -p "$PACKAGE_DIR"
  dotnet pack Source/MarkdownUtil/MarkdownUtil.csproj --configuration Release --no-restore -p:Version="$PACKAGE_VERSION" --output "$PACKAGE_DIR"
  dotnet tool update --global markdownutil --version "$PACKAGE_VERSION" --add-source "$PACKAGE_DIR"
  dotnet tool list --global
  ```
  Increment `N` for repeated local builds on the same date. For a first-time installation, replace `dotnet tool update` with `dotnet tool install`. Restart any running `mdu serve` process to use the updated build.
- Run `dotnet doctor fix` to install this repository's local Git hooks. Use `dotnet doctor diagnose all` to check the hook setup; commit messages must follow Conventional Commits.
- Keep CLI output plain-text and dependency-light. `System.CommandLine` is the command parser; do not reintroduce Spectre.Console.
