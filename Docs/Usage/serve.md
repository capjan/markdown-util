---
sidebar.order: 5
---

# serve

`mdu serve` renders the Markdown site, starts a local preview server, and watches the source folder for changes. Markdown pages and linked local assets are updated as you work. By default, the command opens the preview in a browser and listens on port `4173`. Press `Ctrl+C` to stop it.

## Usage

```bash
mdu serve [PATH [OUTPATH]]
```

For all arguments, options, and defaults, here is the output of `mdu serve --help`:

```text
Description:
  Renders, serves, and watches a Markdown site.

Usage:
  mdu serve [<path> [<outpath>]] [options]

Arguments:
  <path>     Root folder containing the Markdown files. Defaults to ./docs or ./Docs.
  <outpath>  Output folder for the rendered site. Defaults to ./site.

Options:
  -p, --pattern <pattern>        Search pattern for Markdown files. [default: *.md]
  --hidden                       Include hidden files and folders.
  --port <port>                      Local preview server port. [default: 4173]
  --no-browser                       Do not open the preview in a browser.
  --editPageRoot <editPageRoot>      URL to the root URL for editing the source files.
  -?, -h, --help                     Show help and usage information
```

## Examples

Start the default preview, or serve a specific documentation folder on another port:

```bash
mdu serve
mdu serve ./ProjectDocs ./site --port 8080
```

Run the preview without opening a browser:

```bash
mdu serve --no-browser
```

[Back to Usage](README.md)
