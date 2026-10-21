---
navigation:
  title: "Portable Binary"
  order: 2
description: Run MDU from a self-contained executable without installing .NET.
---

# Portable Installation

Each [GitHub release](https://github.com/capjan/mdu/releases/latest) includes self-contained executables that do not require the .NET SDK or runtime. Download the archive for your system, extract it, and run `mdu` or `mdu.exe`. To use the command from any folder, put the extracted executable on your `PATH`.

| Operating system | Architecture | Notes |
| --- | --- | --- |
| Windows | x64 | Run `mdu.exe`. |
| Linux | x64 | Requires glibc 2.35 or newer. Alpine Linux and other musl-based distributions are not supported. |
| macOS | Intel (x64) or Apple silicon (ARM64) | First launch may need an internet connection so Gatekeeper can verify notarization. |

For example, run the executable from its extracted folder:

```bash
./mdu --help
```

On Windows, use `mdu.exe --help` in PowerShell or Command Prompt.

## macOS Gatekeeper

Because the notarization ticket cannot be stapled to the standalone executable, Gatekeeper may need an internet connection to verify it on first launch. If you need to start an official release while offline, first verify the downloaded archive with the accompanying `SHA256SUMS` file. The checksum file covers all release assets, so `--ignore-missing` checks only the files you downloaded:

```bash
shasum -a 256 -c --ignore-missing SHA256SUMS
```

After extracting the archive, you can remove the executable's quarantine attribute if Gatekeeper still blocks it:

```bash
xattr -d com.apple.quarantine ./mdu
```

Removing the quarantine attribute bypasses Gatekeeper's check for this file. Use this fallback only for a trusted release whose checksum you verified.

## Next steps

See the [Usage guide](../Usage/README.md) to lint, preview, and render your documentation.
