---
navigation.visible: false
---

# Rule 2

> [!IMPORTANT]
> The name of a folder that contains a `README.md` must match the title of that `README.md`. Code: `MDU002` DirectoryTitleMatch.

## Why

A `README.md` is the page readers open when they enter a folder. When the folder name matches its title, readers know what the section covers while they browse the repository, and the navigation of the rendered site shows the same name.

## Example

```markdown title="notes/README.md"
# Notes

These are my notes.
```

| Folder | Title | Result |
| --- | --- | --- |
| `notes/` | `# Notes` | :material-check: Matches |
| `Keyboard-Keys/` | `# Keyboard Keys` | :material-check: Matches; case, spaces, hyphens, and underscores are ignored |
| `Rule2/` | `# Rule 2: Folder names` | :material-check: Matches; only the text before the first colon counts |
| `docs/` | `# Overview` | :material-close: Rename the folder or the title |

Set `navigation.title` when a section should show a different label in the navigation.
