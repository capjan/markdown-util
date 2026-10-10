---
sidebar:
  order: 20
---

# Usage

[Dokumentation](../README.md) • Usage

## Quick start

Run commands from the project folder and pass the documentation and output folders explicitly. For a project with a `Docs` directory:

```bash
mdu lint ./Docs
mdu serve ./Docs ./site
```

`mdu lint` reads the Markdown files and reports any problems. `mdu serve` renders the site to `./site`, opens it in your browser, and watches Markdown files and linked local assets for changes. Press `Ctrl+C` to stop the preview server.

For example, a missing local link is reported with its file and line:

```
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
Linter detected 1 error.
```

The source folder, output folder, and port can be specified when needed. The preview defaults to `./site` and port `4173`.

```bash
mdu serve ./Docs ./site --port 4173
```

Use `--no-browser` to start the server without opening a browser.

## Adding breadcrumb navigation

- Generates breadcrumb navigation in every Markdown file in the selected folder and its subfolders.
- Pass the documentation folder from the project root:

```bash
mdu add-breadcrumb-navigation ./Docs
```

Or use the short alias:

```bash
mdu abn ./Docs
```
