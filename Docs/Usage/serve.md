---
sidebar.order: 5
---

# serve

`mdu serve` renders the Markdown site, starts a local preview server, and watches the source folder for changes. Markdown pages and linked local assets are updated as you work. By default, the command opens the preview in a browser. Press `Ctrl+C` to stop it.

## Usage

```bash
mdu serve [PATH [OUTPATH]]
```

See [Paths and file search](README.md#paths-and-file-search) for the default source and output paths, and [common options](README.md#common-options) for `--pattern` and `--hidden`. Run `mdu serve --help` for the current command help.

| Option | Effect |
| --- | --- |
| `--port <port>` | Choose the local preview server port. Default: `4173`. |
| `--no-browser` | Start the server without opening a browser. |
| `--editPageRoot <editPageRoot>` | Add an **Edit Page** link to each rendered page. |

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
