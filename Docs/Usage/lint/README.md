---
sidebar.order: 2
---

# lint

`mdu lint` checks Markdown files under a root folder and reports rule violations with the source file and line number. It exits with code `1` when it finds errors and `0` when it does not.

## Usage

```bash
mdu lint [PATH] [options]
```

The alias `mdu l` runs the same command. See [Paths and file search](../README.md#paths-and-file-search) for the default source path and [common options](../README.md#common-options) for `--pattern` and `--hidden`. Run `mdu lint --help` for the current command help.

## Rules

- See the [Lint Rules Reference](Rules.md) for all rule codes, names, severities, checks, and fixes.
- See also the detailed guides for [Rule 2](Rule2.md), [Rule 3](Rule3.md), and [Rule 5](Rule5.md).

Each diagnostic includes a stable code, a descriptive rule name, the source path, and the line number. Errors make `mdu lint` exit with code `1`; warnings do not. For example, `MDU004` is named `LocalLinkExists`:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```

The linter also lists completely orphaned pages: pages hidden from the sidebar with `sidebar.visible: false` that no visible page links to. This list is informational and does not affect the lint exit code.

## Options

| Option | Effect |
| --- | --- |
| `--pretty` | Group results by file and use paths relative to the lint root. |
| `--color` | Colorize errors and warnings. |

## Automatic fixes

`mdu fix` currently fixes `MDU006` by moving a section's only `README.md` to a same-named Markdown file beside the section folder. It updates links to the page and rebases relative links from the moved page, including links to local assets. See the [fix command documentation](../fix.md) for usage and details.

```bash
mdu fix
mdu fix ./ProjectDocs --pattern "*.markdown"
```

## Examples

Check the default documentation folder:

```bash
mdu lint
```

Check a specific folder or only files matching a different pattern:

```bash
mdu lint ./ProjectDocs
mdu lint ./ProjectDocs --pattern "*.markdown"
mdu lint --pretty --color
```

An error includes the path, line, rule code, and message. For example:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```

[Back to Usage](../README.md)
