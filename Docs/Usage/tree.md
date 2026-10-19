---
navigation.order: 1
---

# tree

`mdu tree` prints the titles of the detected Markdown pages as an indented tree. A folder's `README.md` acts as its parent page; pages in child folders are shown beneath it.

## Usage

```bash
mdu tree [PATH] [options]
```

The alias `mdu print` also runs this command. See [Paths and file search](README.md#paths-and-file-search) for the default source path, and [common options](README.md#common-options) for `--pattern` and `--hidden`. Run `mdu tree --help` for the current command help.

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
