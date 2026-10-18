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

## MDU001: HeadingRequired

Every Markdown page needs a level-one heading (`# Page title`) after any YAML front matter. Add a heading that describes the page when this diagnostic appears.

## MDU002: DirectoryTitleMatch

For a section index named `README.md`, the directory name must match the heading title. The comparison ignores case, punctuation, spaces, hyphens, and underscores. If the title contains a colon, only the text before the first colon is compared.

For example, `# Keyboard Keys` matches the directory `Keyboard-Keys`. See [Rule 2](Rule2.md) for examples and guidance.

## MDU003: LinkCasing

The path in a local link must use the same casing as its target on disk. This catches links that may work on a case-insensitive file system but fail on a case-sensitive one. Query strings and fragments do not affect the path check. Correct the target path to match the file or directory name. See [Rule 3](Rule3.md) for an example.

## MDU004: LocalLinkExists

Relative local links and image targets must point to an existing file or to a directory containing a `README.md`. Query strings and fragments are ignored when resolving the path. External URLs and fragment-only links are not checked by this rule.

Fix the destination or add the missing target. A casing mismatch for an existing target is reported as `MDU003` instead.

## MDU005: DuplicatePageRepresentation

A page cannot have both a Markdown file and a same-named directory containing `README.md`. For example, `Setup.md` and `Setup/README.md` represent the same page; keep one representation. See [Rule 5](Rule5.md) for valid and invalid layouts.

## MDU006: ReadmeWithoutSubpages

This warning appears when a section directory's `README.md` is its only Markdown page and there is no same-named Markdown file beside the directory. The section can be represented by a single file instead. Run `mdu fix` to move the `README.md` beside the directory and update affected links; see [fixing MDU006](../fix.md#fixing-mdu006).

## Informational output

The linter also lists pages marked `sidebar.visible: false` that no visible page links to. These orphaned pages are informational; they do not have a rule code and do not affect the exit code.

[Back to lint](README.md)
