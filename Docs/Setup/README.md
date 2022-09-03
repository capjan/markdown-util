# Setup

[Dokumentation](../README.md) • Setup

For your convenience, the tool is distributed as [.NET Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) via [NuGet](https://www.nuget.org/packages/MarkdownUtil). This simplifies the installation and updating of the tool

## Step 1: Dependencies

Ensure you have installed the required [.NET](https://dotnet.microsoft.com/en-us/download/dotnet) dependency for your operating system



## Step 2: Installation

Install the tool. At will **globally** for your whole system or **locally**
available to a specific folder and it's subfolders.

### Global

```bash
dotnet tool install --global MarkdownUtil
```

### Local

- A local tool can be used in a specific folder and it's subfolders
- It requires a "Tool-Manifest" that provides information about installed tools

```bash
dotnet new tool-manifest # if you are setting up this repo for the first time
dotnet tool install --local MarkdownUtil
```
