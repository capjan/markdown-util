---
sidebar.visible: false
---

# Rule 5

## Summary

A Markdown page must have only one representation. A file such as `Setup.md` and a same-named directory containing `Setup/README.md` represent the same page, so having both is an error (`MDU005`).

A directory whose only Markdown page is its `README.md` is valid, but unnecessary. The linter reports a warning (`MDU006`) because a file such as `Setup.md` is sufficient. The warning does not make `mdu lint` fail.

Run `mdu fix` to move this page to a same-named Markdown file beside its section folder. The command updates links to the page and relative links inside it.

## Examples

Either of these layouts is valid:

```text
Docs/Setup.md
```

```text
Docs/Setup/README.md
```

Having both layouts at once is an error:

```text
Docs/Setup.md
Docs/Setup/README.md
```

A directory README with Markdown subpages remains useful as their parent page and does not trigger the warning.
