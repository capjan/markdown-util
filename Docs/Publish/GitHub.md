---
navigation.order: 1
---

# GitHub

The [mdu GitHub Pages Action](https://github.com/capjan/markdown-util-action) renders your documentation and publishes it to GitHub Pages.

1. In your repository, open **Settings → Pages** and set **Build and deployment → Source** to **GitHub Actions**.
2. Add this workflow. It runs whenever files under `docs/` change on `main`:

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

[Action inputs and details](https://github.com/capjan/markdown-util-action#readme)
