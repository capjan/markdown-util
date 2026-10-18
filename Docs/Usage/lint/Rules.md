---
sidebar:
  visible: false
---

# Lint Rules Reference

`mdu lint` reports diagnostics with a stable rule code and name. Errors make the command exit with code `1`; warnings do not.

## Rule summary

| Code | Name | Severity | Check | More |
| --- | --- | --- | --- | --- |
| `MDU001` | `HeadingRequired` | Error | Every Markdown page has a level-one heading that defines its title. | — |
| `MDU002` | `DirectoryTitleMatch` | Error | A section directory name matches the title of its `README.md`. | [Rule 2](Rule2.md) |
| `MDU003` | `LinkCasing` | Error | The casing in a local link target matches the file-system entry. | [Rule 3](Rule3.md) |
| `MDU004` | `LocalLinkExists` | Error | A local link or image target exists. | — |
| `MDU005` | `DuplicatePageRepresentation` | Error | A page is not represented by both a Markdown file and a same-named directory `README.md`. | [Rule 5](Rule5.md) |
| `MDU006` | `ReadmeWithoutSubpages` | Warning | A section `README.md` is its directory's only Markdown page. | [Rule 5](Rule5.md) |
| `MDU007` | `MaterialAdmonition` | Error | Material admonitions must be converted to GitHub Alerts. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU008` | `MaterialTabs` | Error | Material tabs must be converted to headings. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU009` | `MaterialCards` | Error | Material card grids must be converted to Markdown link lists. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU010` | `UnsupportedMermaid` | Error | A Mermaid diagram uses syntax MarkdownUtil cannot render. | — |
| `MDU011` | `CodeAnnotations` | Error | Material code-annotation markers are unsupported. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU012` | `MkDocsIndexPage` | Error | Rename the MkDocs `index.md` page to `README.md`. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU013` | `MissingSectionReadme` | Error | A section with child pages needs a `README.md` page. | [fix](../fix.md#migrating-mkdocs-material-markdown) |
| `MDU014` | `MkDocsPagesFile` | Error | Migrate MkDocs `.pages` settings to Markdown front matter. | [fix](../fix.md#migrating-mkdocs-material-markdown) |

## MDU001: HeadingRequired

Every Markdown page needs a level-one heading (`# Page title`) after any YAML front matter. Add a heading that describes the page when this diagnostic appears.

## MDU002: DirectoryTitleMatch

For a section index named `README.md`, the directory name must match the heading title. The comparison ignores case, punctuation, spaces, hyphens, and underscores. If the title contains a colon, only the text before the first colon is compared. Set `sidebar.title` when a section intentionally uses a different navigation label; `mdu fix` adds this override when it migrates a mismatched MkDocs section title.

For example, `# Keyboard Keys` matches the directory `Keyboard-Keys`. See [Rule 2](Rule2.md) for examples and guidance.

## MDU003: LinkCasing

The path in a local link must use the same casing as its target on disk. This catches links that may work on a case-insensitive file system but fail on a case-sensitive one. Query strings and fragments do not affect the path check. This also applies to local destinations in supported Mermaid, context, and PlantUML node links. Correct the target path to match the file or directory name. See [Rule 3](Rule3.md) for an example.

## MDU004: LocalLinkExists

Relative local links, image targets, and destinations in supported Mermaid, context, and PlantUML node links must point to an existing file or to a directory containing a `README.md`. Query strings and fragments are ignored when resolving the path. External URLs and fragment-only links are not checked by this rule.

Fix the destination or add the missing target. A casing mismatch for an existing target is reported as `MDU003` instead.

## MDU005: DuplicatePageRepresentation

A page cannot have both a Markdown file and a same-named directory containing `README.md`. For example, `Setup.md` and `Setup/README.md` represent the same page; keep one representation. See [Rule 5](Rule5.md) for valid and invalid layouts.

## MDU006: ReadmeWithoutSubpages

This warning appears when a section directory's `README.md` is its only Markdown page and there is no same-named Markdown file beside the directory. The section can be represented by a single file instead. Run `mdu fix` to move the `README.md` beside the directory and update affected links; see [fixing MDU006](../fix.md#fixing-mdu006).

## MkDocs Material compatibility

Rules `MDU007`–`MDU014` identify MkDocs-specific syntax and structure that needs migration. Run `mdu fix` to convert the supported constructs. An `MDU010` error means a Mermaid diagram uses syntax not currently implemented by MarkdownUtil; update the renderer before migrating that source.

## Informational output

The linter also lists pages marked `sidebar.visible: false` that no visible page links to. These orphaned pages are informational; they do not have a rule code and do not affect the exit code.

[Back to lint](README.md)
