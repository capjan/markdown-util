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

The Linux x64 executable requires glibc 2.35 or newer; Alpine Linux and other musl-based distributions are not supported.

The macOS executables in GitHub releases are signed with a Developer ID Application certificate and notarized by Apple. Gatekeeper checks the notarization on first launch, so connect to the internet then. The notarization ticket cannot be stapled to the standalone executable.

## Configure macOS release signing

The release workflow requires these repository secrets:

- `APPLE_DEVELOPER_ID_APPLICATION_CERTIFICATE_BASE64` — base64-encoded `.p12` export of your **Developer ID Application** certificate and private key.
- `APPLE_DEVELOPER_ID_APPLICATION_CERTIFICATE_PASSWORD` — password used to export the `.p12` file.
- `APPLE_NOTARIZATION_PRIVATE_KEY` — private key (`.p8`) for an App Store Connect API key.

Add these repository variables as well:

- `APPLE_NOTARIZATION_KEY_ID` — the API key ID.
- `APPLE_NOTARIZATION_ISSUER_ID` — the API key issuer ID.

The API key must have the **Developer** role or higher. Create a [Developer ID Application certificate](https://developer.apple.com/help/account/certificates/create-developer-id-certificates/), export it with its private key from Keychain Access, and create an App Store Connect API key under **Users and Access → Integrations**. Store the values in the repository's **Settings → Secrets and variables → Actions**. The workflow stops before publishing macOS artifacts if any are missing.

Apple's guides: [notarizing macOS software](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution) and [customizing the notarization workflow](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow).

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
