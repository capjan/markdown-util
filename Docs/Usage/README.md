# Usage

[Dokumentation](../README.md) • Usage

## Story

- The markdown utility is a command line tool that is easy to set up and is executed in a terminal window
- Open a terminal in the root folder of your documentation to quickly run commands
- For now it's important to be in the root folder because the tool assumes that it is executed in that folder so just stay in that folder

## Running the linter

Start the linter to check your documentation at any time. It's save because it just reads
```bash
mdu lint
```

**Example Output:**

```
Running Linter
Party! 🥳 The Markdown linter does not detect any errors!
```

## Previewing the rendered site

Run `mdu serve` from the project folder containing a `Docs` or `docs` folder. MarkdownUtil renders the site to `./site`, opens it in your browser, and watches for changes. Editing a Markdown file updates its page; changing a linked local asset refreshes the pages that use it. Press `Ctrl+C` to stop the preview server.

```bash
mdu serve
```

The source folder, output folder, and port can be specified when needed. The defaults are `Docs` or `docs`, `site`, and port `4173`.

```bash
mdu serve ./Docs ./site --port 4173
```

Use `--no-browser` to start the server without opening a browser.

## Adding breadcrumb navigation

- Generates Breadcrumb Navigation in every markdown files in the current folder and all subfolders
- Must be executed in the root folder of the documentation

```bash
mdu add-breadcrumb-navigation
```

or in short:

```bash
mdu abn
```
