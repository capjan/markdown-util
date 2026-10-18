---
sidebar.visible: false
---

# Rule 3

## Summary

The casing of internal links must match the casing of their targets on the local file system.

## Goal

When documentation is edited on a case-insensitive file system (such as the default file system on Windows), an incorrectly cased internal link may go unnoticed and later fail on a case-sensitive deployment.

## Example

For example, you might link to the parent page that contains an overview:

```markdown title="Markdown"
Click [here](../README.md) to return to the overview page.
```

If the parent file is actually named `readme.md`, the link may still work on a Windows machine but be broken on Linux, where file name casing matters.
