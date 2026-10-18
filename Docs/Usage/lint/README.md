# lint

`mdu lint` checks Markdown files under a root folder and reports rule violations with the source file and line number. It exits with code `1` when it finds errors and `0` when it does not.

## Usage

```bash
mdu lint [PATH] [options]
```

The alias `mdu l` runs the same command. For all arguments, options, and defaults, here is the output of `mdu lint --help`:

```text
Description:
  Runs the linter and prints the results.

Usage:
  mdu lint [<path>] [options]

Arguments:
  <path>  Root folder containing Markdown files. Defaults to ./docs or ./Docs.

Options:
  -p, --pattern <pattern>  Search pattern for Markdown files. [default: *.md]
  --hidden                 Include hidden files and folders.
  --pretty                 Group lint results by file and use relative paths.
  --color                  Colorize lint results.
  -?, -h, --help           Show help and usage information
```

## Rules

- Rule 1 - Every Markdown page must have a level-one heading that defines its title (after any YAML front matter).
- [Rule 2](Rule2.md) - Directory names must match the title in the index file (`README.md`).
- [Rule 3](Rule3.md) - The casing of internal links in a Markdown file must match the casing of their targets.
- Rule 4 - Local links and image targets must point to an existing file or to a directory with a `README.md` index.
- [Rule 5](Rule5.md) - A page must not be represented by both a Markdown file and a same-named directory README; a directory README without subpages is unnecessary.

Each diagnostic includes a stable code, a descriptive rule name, the source path, and the line number. Errors make `mdu lint` exit with code `1`; warnings do not. For example, `MDU004` is named `LocalLinkExists`:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```

Use `--pretty` to group diagnostics by file and show paths relative to the lint root. Add `--color` to colorize errors and warnings. The linter also lists completely orphaned pages: pages hidden from the sidebar with `sidebar.visible: false` that no visible page links to. This list is informational and does not affect the lint exit code.

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
