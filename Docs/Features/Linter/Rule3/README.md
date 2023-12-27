# Rule 3

[Dokumentation](../../../README.md) • [Features](../../README.md) • [Linter](../README.md) • Rule 3

## Summary

The casing of internal links must match the casing on the local machine.

## Goal

If the documentation is written on machines where the casing of files and folders doesn't matter
(e.g. Windows) the user does not recognize if a used internal link is wrongly cased and will raise
a file not found on a strict deployment where the casing does matter!

## Example

You write your documentation on a windows machine and link to a parent documentation page that contains
an overview:

```markdown
Click [here](../README.md) to return to the overview page
```

If the parent `README.md` is for some reason cased in lowercase (e.g. readme.md), than the link still works
on your windows machine, but may be BROKE if you deploy to a Linux machine.