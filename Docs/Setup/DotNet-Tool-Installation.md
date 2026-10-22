---
navigation:
  title: ".NET Tool"
  order: 1
description: Install MDU as a global or repository-local .NET tool.
---

# .NET Tool Installation

MDU is published on NuGet as the `mdu` [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) and runs as the `mdu` command.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for your operating system. It includes the runtime that MDU needs.

## Install globally

A global tool is available from any folder on your machine:

```bash
dotnet tool install --global mdu
mdu --help
```

Update it later with:

```bash
dotnet tool update --global mdu
```

## Install in a repository

A local tool is pinned in the repository's tool manifest, so everyone on the team and CI can use the same version. Create the manifest once, then install MDU:

```bash
dotnet new tool-manifest
dotnet tool install --local mdu
dotnet mdu --help
```

Commit `.config/dotnet-tools.json`. Others can install the pinned version with:

```bash
dotnet tool restore
```

## Next steps

See the [Usage guide](../Usage/README.md) to lint, preview, and render your documentation.
