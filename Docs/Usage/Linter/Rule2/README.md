# Rule 2

[Dokumentation](../../../README.md) • [Features](../../README.md) • [Linter](../README.md) • Rule 2

## Summary

The name of a directory that contains a readme file must match the title of the readme file.

## Goal

README.md files are index files that are opened first when a developer enters a folder, so they typically contains
the important things to know. But unforgettably they do not contain any information what they contain!

If the name of the folder corresponds to the title of the README file, it can be understood at navigation context 
what the index file addresses.

If a README.md file is located in a docs folder that we can assume that it contains `docs`

## Example

Assume a README.md with the following contant:

```markdown
# notes

this are my Notes
```

It is good practice when the folder is also named `notes`

## Notes

Titles like "Rule 2: This is a following description" is cut at the colon to avoid long names