# PDF Link

`mdu render` and `mdu serve` create a combined `documentation.pdf` in the site output folder only when at least one Markdown page contains a PDF Link. With no PDF Link, no PDF asset is generated. `mdu serve` refreshes the PDF when Markdown pages change.

## Syntax

Place a styled link anywhere in your Markdown and choose its label:

```markdown title="Markdown"
[Download the PDF](mdu:pdf)
```

MDU resolves the link to the generated PDF, including on pages in nested folders.

## Rendered example

[Download the MDU documentation as PDF](mdu:pdf)

The PDF follows the documentation navigation order and honors [`pdf.visible`](../Usage/pdf.md#exclude-pages) in page front matter.

The PDF uses the same Markdown rendering pipeline as [`mdu pdf`](../Usage/pdf.md), including syntax highlighting, math, callouts, and supported diagrams. See [PDF output](../Usage/pdf.md) for the layout and its limitations.
