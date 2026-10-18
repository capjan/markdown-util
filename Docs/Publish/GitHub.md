---
sidebar.order: 1
---

# GitHub

Use the [MarkdownUtil GitHub Pages Action](https://github.com/capjan/markdown-util-action) to render and publish your documentation.

1. In your repository settings, open **Pages** and set **Build and deployment → Source** to **GitHub Actions**.
2. Add `.github/workflows/publish.yml`:

```yaml title=".github/workflows/publish.yml"
name: Publish docs

on:
  push:
    branches: [main]
    paths:
      - 'docs/**'

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  publish:
    runs-on: ubuntu-latest
    environment:
      name: github-pages
    steps:
      - uses: capjan/markdown-util-action@v1
```

The workflow runs only when files under `docs/` change.

[Action inputs and details](https://github.com/capjan/markdown-util-action#readme)
