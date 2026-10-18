---
sidebar.order: 4
---

# render

`mdu render` converts the detected Markdown pages into a static HTML site. It writes the result and copied local assets to the output folder, which defaults to `./site`. The command exits after rendering.

## Usage

```bash
mdu render [PATH [OUTPATH]]
```

The alias `mdu r` runs the same command. See [Paths and file search](README.md#paths-and-file-search) for the default source and output paths, and [common options](README.md#common-options) for `--pattern` and `--hidden`. Run `mdu render --help` for the current command help.

`--editPageRoot` adds an **Edit Page** link to each rendered page and a GitHub repository button in the top bar. The button links to the repository root and shows its `owner/repository` name.

## Examples

Render the default documentation folder to `./site`, or choose both folders explicitly:

```bash
mdu render
mdu render ./ProjectDocs ./public/docs
```

Render only files matching a custom pattern and add links to their source files in a GitHub repository:

```bash
mdu render ./Docs ./site --pattern "*.markdown" --editPageRoot "https://github.com/example/project/edit/main/Docs"
```

The Markdown source files are left unchanged.
