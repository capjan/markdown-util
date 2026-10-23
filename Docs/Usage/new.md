---
navigation.order: 0
---

# new

`mdu new` creates a minimal documentation project with a `docs/README.md` starter page. It does not add configuration or dependencies.

## Usage

```bash
mdu new [DIRECTORY]
```

If `DIRECTORY` is omitted, MDU creates the project in the current directory. Use `.` to make that explicit:

```bash
mdu new .
```

You can also provide a directory that does not exist yet:

```bash
mdu new ./my-docs
```

MDU creates the target directory and its `docs/README.md` page. Existing files are left untouched. If the target already contains a folder named `docs`, MDU stops without changing it.

Run `mdu serve` from the project directory to preview the starter page, or `mdu lint` to check it.
