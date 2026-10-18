# fix

`mdu fix` applies supported automatic fixes to Markdown files. It currently fixes the `MDU006` warning for a section that contains only a `README.md` page.

## Usage

```bash
mdu fix [PATH] [options]
```

For all arguments, options, and defaults, here is the output of `mdu fix --help`:

```text
Description:
  Applies automatic fixes for supported warnings.

Usage:
  mdu fix [<path>] [options]

Arguments:
  <path>  Root folder containing Markdown files. Defaults to ./docs or ./Docs.

Options:
  -p, --pattern <pattern>  Search pattern for Markdown files. [default: *.md]
  --hidden                 Include hidden files and folders.
  -?, -h, --help           Show help and usage information
```

## Fixing MDU006

When a section folder contains only `README.md`, `mdu fix` moves that page beside the folder. For example, `Docs/Rule3/README.md` becomes `Docs/Rule3.md`.

The command updates Markdown links that point to the old page, including links to the section folder and reference links. It also adjusts relative links from the moved page so they continue to point to the same local pages and assets. Query strings and fragments are preserved. If the section folder still contains assets or other files, the folder remains in place.

Run the linter afterward to check the result:

```bash
mdu fix ./ProjectDocs
mdu lint ./ProjectDocs
```

If there are no supported warnings, the command reports that there are no fixable warnings and makes no changes.

[Back to Usage](README.md)
