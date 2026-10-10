# Linter

[Documentation](../../README.md) • [Features](../README.md) • Linter

## Rules

- Rule 1 - Every Markdown page must have a level-one heading that defines its title (after any YAML front matter).
- [Rule 2](./Rule2/README.md) - Directory names must match the title in the index file (`README.md`).
- [Rule 3](./Rule3/README.md) - The casing of internal links in a Markdown file must match the casing of their targets.
- Rule 4 - Local links and image targets must point to an existing file or to a directory with a `README.md` index.

Each diagnostic includes a stable code, a descriptive rule name, the source path, and the line number. For example, `MDU004` is named `LocalLinkExists`:

```text
/project/Docs/Overview.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
```
