---
navigation.order: 75
---

# PDF download link

`mdu render` and `mdu serve` create a combined `documentation.pdf` in the site output folder. Each rendered page has a PDF link in the header, including pages in nested folders.

`mdu serve` refreshes the PDF when Markdown pages change, so the preview always links to the latest version. The PDF follows the documentation navigation order and honors [`pdf.visible`](../Usage/pdf.md#exclude-pages) in page front matter.

The PDF uses the same Markdown rendering pipeline as [`mdu pdf`](../Usage/pdf.md), including syntax highlighting, math, callouts, and supported diagrams. See [PDF output](../Usage/pdf.md) for the layout and its limitations.
