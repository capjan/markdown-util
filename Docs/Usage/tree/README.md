# tree

`mdu tree` prints the titles of the detected Markdown pages as an indented tree. A folder's `README.md` acts as its parent page; pages in child folders are shown beneath it.

## Usage

```bash
mdu tree PATH --pattern PATTERN
```

The alias `mdu print` also runs this command. For all arguments, options, and defaults, here is the output of `mdu tree --help`:

```text
Description:
  Prints the detected Markdown files as a tree.

Usage:
  mdu tree [<path>] [options]

Arguments:
  <path>  Root folder containing Markdown files. Defaults to ./docs or ./Docs.

Options:
  -p, --pattern <pattern>  Search pattern for Markdown files. [default: *.md]
  --hidden                 Include hidden files and folders.
  -?, -h, --help           Show help and usage information
```

## Examples

Print the default documentation tree or choose a folder explicitly:

```bash
mdu tree
mdu tree ./ProjectDocs
```

Use a different file pattern when pages use another extension:

```bash
mdu tree ./ProjectDocs --pattern "*.markdown"
```

[Back to Usage](../README.md)
