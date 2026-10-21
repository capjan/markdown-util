---
navigation.visible: false
---

# Rule 3

> [!IMPORTANT]
> The casing of a local link must match the casing of its target on disk. Code: `MDU003` LinkCasing.

## Why

Windows and macOS usually ignore the case of file names, Linux and most web servers do not. A link with the wrong case works while you write, then breaks after you publish.

## Example

```markdown title="Markdown"
Return to the [overview](../README.md).
```

If the file is actually named `readme.md`, this link works on Windows but fails on a Linux web server. Change the link or rename the file so both use the same case.
