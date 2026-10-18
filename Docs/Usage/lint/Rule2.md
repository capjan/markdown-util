---
sidebar.visible: false
---

# Rule 2

## Summary

The name of a directory containing a `README.md` file must match the title in that file.

## Goal

`README.md` files act as index files that readers open when they enter a directory, so they typically contain
the most important information about it. However, their titles may not indicate what the directory contains.

When the directory name matches the README title, readers can tell what the index covers while navigating the project.

For example, if a `README.md` file is in a `docs` directory, its title should be `Docs`.

## Example

Assume a `README.md` file with the following content:

```markdown title="Markdown"
# notes

These are my notes.
```

It is good practice to name the directory `notes` as well.

## Notes

Titles such as "Rule 2: This is a following description" are truncated at the colon to avoid long directory names.
