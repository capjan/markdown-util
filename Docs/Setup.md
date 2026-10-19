---
navigation.order: 10
navigation.placement: top
---

# Setup

MDU is distributed on NuGet as the `MarkdownUtil` [.NET tool package](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools). It requires .NET 10.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for your operating system. The SDK includes the .NET runtime that MDU needs.

## Installation

Install the tool globally for your system or locally for a repository.

### Global

```bash
dotnet tool install --global MarkdownUtil
```

Check that the command is available:

```bash
mdu --help
```

### Local

- A local tool is available in the repository and its subfolders.
- A local tool requires a tool manifest.

```bash
dotnet new tool-manifest # if you are setting up this repo for the first time
dotnet tool install --local MarkdownUtil
```

Run the local tool from the repository with `dotnet mdu`, for example:

```bash
dotnet mdu --help
```
