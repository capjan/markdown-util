# render

`mdu render` converts the detected Markdown pages into a static HTML site. It writes the result and copied local assets to the output folder, which defaults to `./site`. The command exits after rendering.

## Usage

```bash
mdu render [PATH [OUTPATH]]
```

The alias `mdu r` runs the same command. For all arguments, options, and defaults, here is the output of `mdu render --help`:

```text
Description:
  Renders Markdown files to a static HTML site.

Usage:
  mdu render [<path> [<outpath>]] [options]

Arguments:
  <path>     Root folder containing Markdown files. Defaults to ./docs or ./Docs.
  <outpath>  Output folder for rendered files. Defaults to ./site.

Options:
  -p, --pattern <pattern>        Search pattern for Markdown files. [default: *.md]
  --hidden                       Include hidden files and folders.
  --editPageRoot <editPageRoot>      URL to the root URL for editing the source files.
  -?, -h, --help                 Show help and usage information
```

`--editPageRoot` adds an **Edit Page** link to each rendered page.

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

[Back to Usage](README.md)
