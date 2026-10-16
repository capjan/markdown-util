# lint

`mdu lint` checks Markdown files under a root folder and reports rule violations with the source file and line number. It exits with code `1` when it finds errors and `0` when it does not.

## Usage

```bash
mdu lint [PATH]
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
  -?, -h, --help           Show help and usage information
```

## Rules

- Rule 1 - Every Markdown page must have a level-one heading that defines its title (after any YAML front matter).
- [Rule 2](./Rule2/README.md) - Directory names must match the title in the index file (`README.md`).
- [Rule 3](./Rule3/README.md) - The casing of internal links in a Markdown file must match the casing of their targets.
- Rule 4 - Local links and image targets must point to an existing file or to a directory with a `README.md` index.

Each diagnostic includes a stable code, a descriptive rule name, the source path, and the line number. For example, `MDU004` is named `LocalLinkExists`:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```

The linter also lists completely orphaned pages: pages hidden from the sidebar with `sidebar.visible: false` that no visible page links to. This list is informational and does not affect the lint exit code.

## Examples

Check the default documentation folder:

```bash
mdu lint
```

Check a specific folder or only files matching a different pattern:

```bash
mdu lint ./ProjectDocs
mdu lint ./ProjectDocs --pattern "*.markdown"
```

An error includes the path, line, rule code, and message. For example:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```

[Back to Usage](../README.md)
