---
navigation.order: 5
---

# serve

`mdu serve` renders the site and its `documentation.pdf`, starts a local preview server, and opens it in your browser. While it runs, it watches the documentation folder: when you save a page or a linked image, the affected pages are rendered again and the browser reloads them. Markdown changes also refresh the PDF. Press ++ctrl+c++ to stop.

## Usage

```bash
mdu serve [PATH [OUTPATH]] [options]
```

See [Paths and file search](README.md#paths-and-file-search) for the default folders and [common options](README.md#common-options) for `--pattern` and `--hidden`.

| Option | Effect |
| --- | --- |
| `--port <port>` | Port of the preview server. Default: `4173`. |
| `--no-browser` | Start the server without opening a browser. |
| `--editPageRoot <url>` | Add edit links and a repository button, as with [`mdu render`](render.md). |

## Examples

```bash
mdu serve
mdu serve ./ProjectDocs ./site --port 8080
mdu serve --no-browser
```

> [!NOTE]
> The output folder must differ from the documentation folder and must not contain it. Use `mdu render` to create the site you publish; `serve` is meant for local preview.

[How live reload works](../Architecture/Pipeline.md#live-preview) describes what `serve` renders again after a change.
