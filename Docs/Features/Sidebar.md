# Navigation

MarkdownUtil builds its navigation from the Markdown file structure. A `README.md` represents its folder, and child pages appear beneath it. The sidebar is a collapsible view of that tree. `navigation.title` sets the navigation label, `navigation.order` sorts entries, `navigation.visible` controls whether an entry appears, and `navigation.expanded` sets the initial state of collapsible sidebar sections.

## Top navigation

Set `navigation.placement: top` on a page at the documentation root, or on the `README.md` or `index.md` of a first-level folder, to make it a top navigation tab. The root `README.md` remains the landing page by default; mark it `top` if it should also appear as a tab such as Dashboard.

`navigation.placement` controls the preferred position of a first-level entry. On wide screens, `top` entries appear in the tab bar and only the active tab's descendants appear in the sidebar; the tab itself and the documentation root are not repeated there. If the active tab has no child pages, the sidebar is hidden. On compact screens, the tab bar is folded into the full sidebar tree at each entry's original place. Selecting a tab does not change the tree root, and breadcrumbs keep the full path from the documentation root. A top-level Markdown file can also be a tab, but its sidebar section has no child pages.

On wide screens, the tabs appear below the site header. A full-width page bar follows them; its menu button sits above the sidebar. At viewport widths up to 1220 px, mdu hides the tab bar and shows the full documentation tree in the sidebar. The selected section opens in place. On narrow screens, the sidebar opens as a drawer.

```text
Docs/
├── README.md
├── Development/
│   ├── README.md  # navigation.placement: top
│   └── Setup.md
└── Operations.md  # navigation.placement: top
```

For example, this front matter opts the Development section into the top navigation:

```yaml
---
navigation:
  placement: top
  title: Development
  order: 10
---
```

The order of tabs uses the same `navigation.order` setting as other entries. If no order is set, entries sort alphabetically by their navigation title. `navigation.title` overrides the title used in the sidebar, tabs, and breadcrumbs; it does not change the page's `title` or heading.

## Ordering entries

Put navigation settings in the Markdown file's YAML front matter. Smaller `navigation.order` values appear first among siblings. Entries without an order follow ordered entries and sort alphabetically by title.

```yaml
---
navigation:
  order: 10
---
```

The compact form `navigation.order: 10` is also supported. When both forms are present, the nested `navigation` value takes precedence.

## Expanding a section

Set `navigation.expanded: true` on a page with child pages to have its sidebar section open by default:

```yaml
---
navigation:
  expanded: true
---
```

This setting applies to collapsible sidebar sections. A selected top navigation root is always expanded, so setting `navigation.expanded` on it has no effect and is a lint error. A section leading to the current page still expands automatically, and manual changes remain in effect while navigating during the current session.

## Hiding an entry

Set `navigation.visible: false` to omit an entry from the sidebar and top navigation. The page remains available by its URL. If the page represents a folder, its child pages remain visible at the next higher level:

```yaml
---
navigation:
  visible: false
---
```

`mdu lint` rejects the retired `sidebar.*` front matter. Replace it with `navigation.*` in the Markdown files; `mdu fix` does not migrate this field.
