---
sidebar.order: 20
---

# Usage

## Quick start

Run commands from the project folder. Commands that read Markdown use `./docs` or `./Docs` by default. `render` and `serve` write the generated site to `./site`. For a project with a `Docs` directory:

```bash
mdu lint
mdu fix
mdu render
mdu serve
```

`mdu lint` reads the Markdown files and reports any problems. `mdu fix` applies supported automatic fixes and updates affected links. `mdu render` writes the static site to `./site` and exits. `mdu serve` renders the site to `./site`, opens it in your browser, and watches Markdown files and linked local assets for changes. Press `Ctrl+C` to stop the preview server.

Both `render` and `serve` generate breadcrumb navigation in the HTML from the Markdown file structure. The Markdown files are left unchanged.

For example, a missing local link is reported with its file and line:

```
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
Linter detected 1 error.
```

You can specify the source directory, output directory, or a different port when needed. The preview defaults to `./site` and port `4173`.

```bash
mdu render ./ProjectDocs ./public/docs
mdu serve --port 8080
```

Use `--no-browser` to start the server without opening a browser.

## Commands

| Command | Purpose | Details |
| --- | --- | --- |
| `mdu lint [path]` | Checks Markdown pages and local links. Alias: `mdu l`. | [lint](./lint/README.md) |
| `mdu fix [path]` | Applies automatic fixes for supported warnings. | [fix](fix.md) |
| `mdu render [path] [outpath]` | Writes a static HTML site. Alias: `mdu r`. | [render](render.md) |
| `mdu serve [path] [outpath]` | Serves a local preview and updates it when files change. | [serve](serve.md) |
| `mdu tree [path]` | Prints the detected Markdown page structure. Alias: `mdu print`. | [tree](tree.md) |

For `path`, MarkdownUtil uses the only `./docs` or `./Docs` directory in the current folder when one exists. If neither exists, or both exist, pass the path explicitly. `render` and `serve` use `./site` as the default `outpath`. Commands that search for Markdown files use `*.md` by default; use `--pattern` (or `-p`) to choose another file pattern.
