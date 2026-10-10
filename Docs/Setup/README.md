---
sidebar:
  order: 10
---

# Setup

[Dokumentation](../README.md) • Setup

MarkdownUtil is distributed as a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) through [NuGet](https://www.nuget.org/packages/MarkdownUtil). It requires .NET 10.

## Step 1: Dependencies

Install the [.NET 10 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for your operating system.

## Step 2: Installation

Install the tool globally for your system or locally for a repository.

### Global

```bash
dotnet tool install --global MarkdownUtil
```

### Local

- A local tool is available in the repository and its subfolders.
- A local tool requires a tool manifest.

```bash
dotnet new tool-manifest # if you are setting up this repo for the first time
dotnet tool install --local MarkdownUtil
```
