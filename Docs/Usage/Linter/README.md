# Linter

[Dokumentation](../../README.md) • [Features](../README.md) • Linter

## Rules

- Rule 1 - Every Markdown page must have a level-one heading that defines its title (after optional YAML frontmatter)
- [Rule 2](./Rule2/README.md) - Directory names must match the title of the index (README.md) file
- [Rule 3](./Rule3/README.md) - Casing of internal links used in a markdown file must match the casing of the current machine
- Rule 4 - Local links and image targets must point to an existing file or a directory with a `README.md` index

Each diagnostic includes a stable code, a descriptive rule name, the source path, and the line number. For example, `MDU004` is `LocalLinkExists`:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```
