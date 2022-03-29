# Install

[Dokumentation](../README.md) • Install

Markdown Util is deployed as .NET Tool.

## Step 1: Dependencies

Ensure you have installed the required dependencies for your operating system

- [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

## Step 2: Installation

Now you can install the tool **globally** to your operating system or **locally**
to a folder and it's subfolders.

### Global

```bash
dotnet tool install --global MarkdownUtil
```

### Local

- A Local Tool can be used in a specific folder and it's subfolders
- It requires a Tool-Manifest that provides the information about installed tools

```bash
dotnet new tool-manifest # if you are setting up this repo for the first time
dotnet tool install --local MarkdownUtil
```
