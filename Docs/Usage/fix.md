---
sidebar.order: 3
---

# fix

`mdu fix` migrates supported MkDocs Material syntax to portable Markdown, normalizes MkDocs page structure, and applies supported linter fixes. Review its changes before publishing.

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

## Migrating MkDocs Material Markdown

The linter reports Material admonitions, tabs, card grids, code annotations, `index.md` pages, missing section `README.md` pages, and `.pages` files as migration issues. `mdu fix` converts these where possible:

- Admonitions and collapsible details become GitHub Alerts. Their contents remain visible; unknown admonition types become `NOTE`.
- Tabs become headings, and card grids become Markdown link lists with their descriptions.
- `{ .annotate }` markers are removed while code comments and numbered explanations stay in the document.
- `index.md` becomes `README.md`; missing section `README.md` pages are created from their directory names.
- `.pages` titles and ordering become `sidebar.title` and `sidebar.order` front matter, then `.pages` files are removed.
- Blog and tag indexes become static Markdown link lists.

Mermaid blocks are checked against the syntax supported by MarkdownUtil. Unsupported syntax remains unchanged and is reported as an error so it can be reviewed. `mdu fix` does not generate diagram replacements.
