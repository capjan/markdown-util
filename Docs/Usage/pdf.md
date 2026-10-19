---
navigation.order: 45
---

# pdf

`mdu pdf` combines the Markdown files under one source folder into a single local PDF. It uses the same Markdown pipeline as `mdu render`, including MarkdownUtil's code highlighting, icons, keyboard keys, math, alerts, and supported diagrams. PDF output is generated directly in MarkdownUtil without a PDF package or network service.

```text
mdu pdf [PATH [OUTPATH]]
```

`PATH` can be a documentation folder or one Markdown file. It defaults to the only `./docs` or `./Docs` folder in the current directory. `OUTPATH` is a PDF file; it defaults to `./documentation.pdf` for a folder or to a PDF named after the selected Markdown file.

```bash
mdu pdf
mdu pdf ./ProjectDocs ./build/manual.pdf
mdu pdf ./ProjectDocs/Features/Math.md ./build/math.pdf
```

The renderer creates A4 pages in the order used by the documentation navigation. A folder creates a title page and a linked table of contents. A single Markdown file creates a PDF of that page without the front matter pages. Set `pdf.visible: false` in a file's front matter to leave it out of a folder export and its table of contents:

```yaml
---
pdf.visible: false
---
```

The nested form `pdf: { visible: false }` is also supported.

Headings stay with the start of the following block when a page break is needed. Markdown links and links on supported diagram nodes remain clickable when their targets are included in the PDF; external links remain clickable too. It does not download web content: images are represented by their alt text, remote image URLs remain clickable, and remote media is represented by its link. Emoji shortcodes are written as shortcodes because the built-in PDF fonts do not contain emoji glyphs. The interactive icon search is available in HTML output only.

Use `--pattern` to choose another Markdown filename pattern and `--hidden` to include hidden files and folders. Run `mdu pdf --help` for all options.
