# Repository Imports

MDU can combine Markdown pages from several GitHub repositories into one documentation site. Add an import declaration to the front matter of a page where the imported section should appear:

```yaml title="docs/API/README.md"
---
title: API reference
mdu:
  import:
    source: https://github.com/example/api-docs/tree/main/Docs
navigation:
  order: 20
---
```

Copy the GitHub `tree` URL for the folder into `source`. That URL identifies the repository, branch or tag, and folder. Branch names containing `/` are supported.

MDU imports the folder's page tree at the declaration's position. The imported folder must contain a `README.md` or `index.md` at its root. The declaration page's front matter controls the root page metadata; keep the declaration page's body empty and put its content in the imported root page instead. Front matter on imported child pages is preserved. Imports declared on local child pages are expanded recursively; imports declared inside the imported repository are rejected.

Edit links on imported pages point to their source files in GitHub and are derived from the same `tree` URL. Repository imports are resolved by `mdu render`, `mdu serve`, `mdu tree`, `mdu lint`, and `mdu pdf`.

Git must be installed. Public repositories work without credentials; private repositories use the authentication already configured for Git.
