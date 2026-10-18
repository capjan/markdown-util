# Sidebar

## Overview

When you run `mdu serve`, MarkdownUtil creates a collapsible sidebar from the Markdown files in the documentation folder. A `README.md` represents its folder in the navigation, and child folders appear nested below it. The root documentation page stays expanded; other pages with child pages have a disclosure button to expand or collapse them. These sections are collapsed by default when the site is first opened. Set `sidebar.expanded` to `true` in a page's front matter to expand its section by default. Selecting a nested page still expands its section and the sections leading to it automatically. The current page stays highlighted in gray until hovered, when it uses the hover color. Expanded sections stay open while you navigate during the current session.

By default, entries at the same level are sorted alphabetically by page title. Add a `sidebar.order` number to a page's YAML front matter to choose its position. Smaller numbers appear first. Pages with an order are listed before pages without one; unordered pages remain alphabetical. If two pages have the same order, they are sorted alphabetically by title.

## Top navigation

When the documentation root has a `README.md`, its visible child pages also appear as horizontal tabs at the top of each page. The root `README.md` itself is not a tab. A top-level section stays selected while you view one of its child pages. Tabs use the same labels and ordering as the sidebar: set `sidebar.title` to change a label, `sidebar.order` to change its position, and `sidebar.visible: false` to hide it. No separate top-navigation setting is needed.

For example, this structure creates **Guides** and **Reference** tabs:

```text
Docs/
├── README.md
├── Guides/
│   └── README.md
└── Reference/
    └── README.md
```

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
| `Docs/Architecture/README.md` | 35 |
| `Docs/OtherTools.md` | 40 |

For a single setting, you can also use the compact form:

```yaml
---
sidebar.order: 10
---
```

MarkdownUtil recognizes `sidebar.order` as a compact alternative to the nested form. If both `sidebar: { order: ... }` and `sidebar.order: ...` are present, the nested form takes precedence.

The numbers only order siblings at the same level. For example, the value in `Docs/Features/README.md` positions the Features section among the other top-level sections. Values in files beneath `Docs/Features/` control their positions inside that section.

## Expanding a section by default

Set `sidebar.expanded` to `true` on a page with child pages to have its section open by default:

```yaml
---
sidebar:
  expanded: true
---
```

The compact form `sidebar.expanded: true` is also supported. Sections are collapsed by default when this setting is omitted. The setting provides the initial state; a section leading to the current page still expands automatically, and manual changes remain in effect while navigating during the current session.

## Hiding a page

Set `sidebar.visible` to `false` to omit a page from the sidebar. Pages are visible by default. If the page represents a folder, its child pages remain visible at the next higher level:

```yaml
---
sidebar:
  visible: false
---
```
