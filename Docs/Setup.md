---
navigation.order: 10
navigation.placement: top
---

# Setup

MDU is published on NuGet as the `MarkdownUtil` [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) with the command `mdu`.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for your operating system. It includes the runtime that MDU needs.

## Download a standalone executable

Each GitHub release includes self-contained executables for Windows x64, Linux x64, macOS Intel, and Apple silicon (ARM64). They do not require the .NET SDK or runtime. Download the archive for your system from the [latest release](https://github.com/capjan/mdu/releases/latest), extract it, and put `mdu` or `mdu.exe` on your `PATH`.

## Install globally

A global tool is available everywhere on your machine:

```bash
dotnet tool install --global MarkdownUtil
mdu --help
```

Update it later with `dotnet tool update --global MarkdownUtil`.

## Install in a repository

A local tool is pinned in the repository's tool manifest, so everyone on the team and the CI use the same version:

```bash
dotnet new tool-manifest   # only once per repository
dotnet tool install --local MarkdownUtil
dotnet mdu --help
```

> [!TIP]
> Commit `.config/dotnet-tools.json`. Others then install the pinned version with `dotnet tool restore`.

## Next steps

[Usage](./Usage/README.md) - Lint and preview your first site
