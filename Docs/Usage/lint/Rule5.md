---
navigation.visible: false
---

# Rule 5

> [!IMPORTANT]
> A page must have only one representation. Codes: `MDU005` DuplicatePageRepresentation (error) and `MDU006` ReadmeWithoutSubpages (warning).

## Examples

Either layout is valid on its own:

```text
docs/Setup.md
```

```text
docs/Setup/README.md
```

Both at once are an error, `MDU005`, because they describe the same page:

```text
docs/Setup.md
docs/Setup/README.md
```

## A folder with only a README

A folder whose only Markdown page is its `README.md` is valid but unnecessary, because `Setup.md` would do the same. The linter reports the warning `MDU006`, which does not make `mdu lint` fail.

> [!TIP]
> Run [`mdu fix`](../fix.md#fixing-mdu006) to move the page to a same-named file beside the folder. It updates the links to the page and the relative links inside it.

A `README.md` with Markdown subpages is the parent page of its section and does not trigger the warning.
