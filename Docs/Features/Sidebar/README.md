# Sidebar

## Overview

When you run `mdu serve`, MarkdownUtil creates a collapsible sidebar from the Markdown files in the documentation folder. A `README.md` represents its folder in the navigation, and child folders appear nested below it.

By default, entries at the same level are sorted alphabetically by page title. Add a `sidebar.order` number to a page's YAML front matter to choose its position. Smaller numbers appear first. Pages with an order are listed before pages without one; unordered pages remain alphabetical. If two pages have the same order, they are sorted alphabetically by title.

## Setting the order

Put the `sidebar` field at the top of the Markdown file, before its title. For example, this front matter in `Docs/Setup/README.md` places Setup before the other top-level sections:

```yaml
---
sidebar:
  order: 10
---
```

The MarkdownUtil documentation uses this ordering for its top-level sections:

| Page | `sidebar.order` |
| --- | ---: |
| `Docs/Setup/README.md` | 10 |
| `Docs/Usage/README.md` | 20 |
| `Docs/Features/README.md` | 30 |
| `Docs/OtherTools/README.md` | 40 |

For a single setting, you can also use the compact form:

```yaml
---
sidebar.order: 10
---
```

MarkdownUtil recognizes `sidebar.order` as a compact alternative to the nested form. If both `sidebar: { order: ... }` and `sidebar.order: ...` are present, the nested form takes precedence.

The numbers only order siblings at the same level. For example, the value in `Docs/Features/README.md` positions the Features section among the other top-level sections. Values in files beneath `Docs/Features/` control their positions inside that section.

## Hiding a page

Set `sidebar.visible` to `false` to omit a page from the sidebar. Pages are visible by default. If the page represents a folder, its child pages remain visible at the next higher level:

```yaml
---
sidebar:
  visible: false
---
```
