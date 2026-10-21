---
navigation.order: 45
---

# pdf

`mdu pdf` combines the Markdown pages of a folder into one PDF file. It uses the same Markdown pipeline as `mdu render`, so code highlighting, icons, keyboard keys, math, callouts, and supported diagrams appear in the PDF too. MDU writes the file itself, without a PDF library, browser, or network service.

For automatic PDF generation during `mdu render` and `mdu serve`, see the [PDF download link feature](../Features/Pdf-Download.md).

## Usage

```bash
mdu pdf [PATH [OUTPATH]] [options]
```

| Argument | Meaning | Default |
| --- | --- | --- |
| `PATH` | A documentation folder or one Markdown file | `./docs` |
| `OUTPATH` | The PDF file to write | `./documentation.pdf`, or the Markdown file's name with `.pdf` |

See [common options](README.md#common-options) for `--pattern` and `--hidden`.

```bash
mdu pdf
mdu pdf ./ProjectDocs ./build/manual.pdf
mdu pdf ./ProjectDocs/Features/Math.md ./build/math.pdf
```

## Layout

- Pages are A4 and follow the order of the documentation navigation.
- A folder export starts with a title page and a linked table of contents; a single file has neither.
- Headings stay with the start of the following block when a page break is needed.
- Links between included pages, links on diagram nodes, and external links stay clickable.

## Exclude pages

To leave a page out of a folder export and its table of contents, set `pdf.visible` in its front matter:

```yaml
---
pdf.visible: false
---
```

The nested form `pdf: { visible: false }` works as well.

## Limitations

The PDF contains only what MDU can draw without downloading anything:

- Images appear as their alt text; remote image URLs stay clickable.
- Embedded videos appear as their link.
- Emoji shortcodes stay text, because the built-in PDF fonts have no emoji glyphs.
- The interactive icon search and the animated hero exist only in HTML; the hero's title, text, and button labels appear as plain text.
