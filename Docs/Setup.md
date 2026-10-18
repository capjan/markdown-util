---
sidebar.order: 10
---

# Setup

MarkdownUtil is distributed as a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) through [NuGet](https://www.nuget.org/packages/MarkdownUtil). It requires .NET 10.

## Step 1: Install the .NET 10 SDK

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for your operating system. The SDK includes the .NET runtime that MarkdownUtil needs.

## Step 2: Installation

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
