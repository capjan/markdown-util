---
sidebar.order: 3
---

# fix

`mdu fix` applies supported automatic fixes to Markdown files. It currently fixes the `MDU006` warning for a section that contains only a `README.md` page.

## Usage

```bash
mdu fix [PATH] [options]
```

See [Paths and file search](README.md#paths-and-file-search) for the default source path and [common options](README.md#common-options) for `--pattern` and `--hidden`. Run `mdu fix --help` for the current command help.

## Fixing MDU006

When a section folder contains only `README.md`, `mdu fix` moves that page beside the folder. For example, `Docs/Rule3/README.md` becomes `Docs/Rule3.md`.

The command updates Markdown links that point to the old page, including links to the section folder and reference links. It also adjusts relative links from the moved page so they continue to point to the same local pages and assets. Query strings and fragments are preserved. If the section folder still contains assets or other files, the folder remains in place.

`mdu fix` changes Markdown files and links when it applies a fix. Review those changes, then run the linter again:

```bash
mdu fix ./ProjectDocs
mdu lint ./ProjectDocs
```

If there are no supported warnings, the command reports that there are no fixable warnings and makes no changes.

[Back to Usage](README.md)
