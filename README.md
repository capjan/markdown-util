# MarkdownUtil

[![.NET 10](https://github.com/capjan/markdown-util/actions/workflows/dotnet.yml/badge.svg)](https://github.com/capjan/markdown-util/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/MarkdownUtil)](https://www.nuget.org/packages/MarkdownUtil)

MarkdownUtil is a .NET command-line tool for documentation kept in ordinary Markdown files. It checks Markdown structure and links, renders a static HTML site, and provides a local preview that updates as files change.
It uses the .NET toolchain without requiring a separate Node.js or Python toolchain.

**Project status:** Draft. The tool is under active development and is not recommended for production environments yet.

## Install

MarkdownUtil requires .NET 10. Install it globally with:

```bash
dotnet tool install --global MarkdownUtil
```

To install it only for the current repository, create a tool manifest once if the repository does not already have one, then install the local tool:

```bash
dotnet new tool-manifest # skip this if .config/dotnet-tools.json already exists
dotnet tool install --local MarkdownUtil
```

Run a locally installed tool with `dotnet tool run mdu`; a global installation uses `mdu` directly.

## Get started

Create `Docs/README.md` with a level-one heading and some content, for example:

```markdown
# My documentation

This is my first page.
```

From the project folder, run the linter and start the preview. Use `mdu` for a global installation or `dotnet tool run mdu` for a local installation:

```bash
mdu lint ./Docs
mdu serve ./Docs ./site
```

```bash
dotnet tool run mdu lint ./Docs
dotnet tool run mdu serve ./Docs ./site
```

`mdu serve` opens the generated site in your browser, serves it on port `4173`, and refreshes pages when Markdown files or linked local assets change. Press `Ctrl+C` to stop it. The source and output folders can be changed with the positional arguments shown above.

When the linter finds a problem, it prints the source path, line number, and rule code:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
Linter detected 1 error.
```

See the [project documentation](./Docs/README.md) for installation details, command usage, and feature descriptions, or browse the [rendered documentation](https://capjan.github.io/markdown-util/).
