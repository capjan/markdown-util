---
sidebar.order: 20
---

# Usage

## Quick start

Run these commands from your project folder. `serve` is for local preview; use `render` when you are ready to create the site for publishing, or `pdf` to create one local document.

```bash
mdu tree
mdu lint
mdu serve
mdu pdf
```

`mdu tree` shows the detected page structure. `mdu lint` reports problems. If it reports an `MDU006` warning, see [fix](fix.md) for the automatic fix; it moves a Markdown page and updates affected links. Run `mdu lint` again afterward. `mdu serve` renders the site, opens it in your browser, and watches Markdown files and linked local assets for changes. Press `Ctrl+C` to stop the preview server. When you are ready to publish, run `mdu render` to create the static site in `./site`.

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
| `mdu tree [path]` | Prints the detected Markdown page structure. Alias: `mdu print`. | [tree](tree.md) |
| `mdu lint [path]` | Checks Markdown pages and local links. Alias: `mdu l`. | [lint](./lint/README.md) · [rules reference](./lint/Rules.md) |
| `mdu fix [path]` | Applies automatic fixes for supported warnings. | [fix](fix.md) |
| `mdu render [path] [outpath]` | Writes a static HTML site. Alias: `mdu r`. | [render](render.md) |
| `mdu pdf [path] [outpath]` | Writes one local PDF file. | [pdf](pdf.md) |
| `mdu serve [path] [outpath]` | Serves a local preview and updates it when files change. | [serve](serve.md) |

## Paths and file search

Commands that read Markdown use the only `./docs` or `./Docs` directory in the current folder by default. If neither exists, or both exist, pass the source path explicitly. `render` and `serve` write to `./site` by default. `pdf` writes `./documentation.pdf`. All six commands search for `*.md` files unless you choose another pattern.

## Common options

These options are available on `tree`, `lint`, `fix`, `render`, `pdf`, and `serve`:

| Option | Effect |
| --- | --- |
| `-p, --pattern <pattern>` | Search for files matching the pattern. Default: `*.md`. |
| `--hidden` | Include hidden files and folders in the search. |
| `-?, -h, --help` | Show help for the command. |

Some commands have additional options; see their command pages. Run `mdu <command> --help` to see the current help output.
