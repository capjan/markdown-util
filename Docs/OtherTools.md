---
navigation.order: 40
navigation.placement: top
---

# Other Tools

MDU focuses on one job: publishing a folder of plain Markdown with a single .NET tool and no theme or plugin setup. If you need a plugin ecosystem, custom components, versioned documentation, or translations, one of these generators may suit you better.

| Tool | Built on | Typical use |
| --- | --- | --- |
| [Docusaurus](https://docusaurus.io/) | React, Node.js | Product documentation with MDX components, versions, and translations |
| [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/) | MkDocs, Python | Feature-rich MkDocs theme; end of life on **May 5, 2027** |
| [MkDocs](https://www.mkdocs.org/) | Python | Simple project documentation configured in one YAML file |
| [Rspress](https://rspress.rs/) | React, Rsbuild | Fast documentation sites with MDX |
| [Starlight](https://starlight.astro.build/) | Astro | Documentation sites that mix Markdown with Astro components |
| [VuePress](https://vuepress.vuejs.org/) | Vue | Documentation with Vue components in Markdown |
| [Zensical](https://zensical.org/) | Python, Rust | Successor to Material for MkDocs from the same team |

> [!TIP]
> Coming from Material for MkDocs? MDU understands the same icon shortcodes, and [`mdu fix`](./Usage/fix.md#migrating-mkdocs-material-markdown) converts admonitions, tabs, card grids, and `.pages` files to portable Markdown.
