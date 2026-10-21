---
navigation.order: 2
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

The linter also lists completely orphaned pages: pages hidden from navigation with `navigation.visible: false` that no visible page links to. This list is informational and does not affect the lint exit code.

## Options

| Option | Effect |
| --- | --- |
| `--pretty` | Group results by file and use paths relative to the lint root. |
| `--color` | Colorize errors and warnings. |
| `--format <format>` | `text` (default), `json` for scripts and tools, or `github` for annotations in GitHub Actions. `--pretty` and `--color` apply to `text` only. |

## Output for tools and CI

`--format json` prints one JSON document and nothing else, so scripts can read it directly. Paths in `file` are relative to the lint root; `path` is the full path. `code` and `rule` come from the diagnostic, and `rule` is `null` for rules whose messages carry only a code.

```json
{
  "root": "/project/Docs",
  "errors": 1,
  "warnings": 0,
  "diagnostics": [
    {
      "file": "Overview.md",
      "path": "/project/Docs/Overview.md",
      "line": 7,
      "severity": "error",
      "code": "MDU004",
      "rule": "LocalLinkExists",
      "message": "Local link target './intro.md' does not exist."
    }
  ],
  "orphanedPages": []
}
```

`--format github` writes [workflow commands](https://docs.github.com/actions/using-workflows/workflow-commands-for-github-actions) that GitHub Actions shows as annotations on the affected lines of a pull request. Paths are relative to the working directory, so run the command from the repository root:

```yaml
- name: Lint documentation
  run: mdu lint --format github
```

The exit code is the same for every format: `1` when there are errors, otherwise `0`.

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
