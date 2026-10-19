# HTML Renderer

## Features and goals

- Light and dark themes, with an automatic mode that follows the operating system.
- A collapsible, responsive sitemap navigation.
- Responsive, mobile-first layouts.
- Syntax highlighting for code blocks.
- GitHub-compatible [callouts](../Features/Callouts.md) for notes, tips, and warnings.
- [Icons and emojis](../Features/Icons-and-Emojis.md) from shortcodes, with a searchable icon index.
- [Keyboard keys](../Features/Keyboard-Keys/README.md) drawn as keycaps with their usual symbols.
- Client-side search with direct links to matching sections and highlighted occurrences.
- Automatic copying of assets to the output directory.
- Breadcrumb navigation generated from the Markdown file structure.
- Top-level navigation tabs selected with `navigation.placement: top`; the setting controls a section's preferred navigation position. Wide layouts show only the active tab's descendants in the sidebar and hide the sidebar when that tab has no children. Narrow viewports fold the tabs into the full sidebar tree while preserving its documentation root and breadcrumb path. A full-width page bar holds the menu control, breadcrumbs, and page contents menu. Site branding and theme colors come from `mdu.yml`, and page metadata comes from YAML front matter.
- Dependency-free image lightboxes and hover/focus footnote previews.
 
## Technical details

- The embedded HTML template is rendered by `HtmlRenderer` using .NET string and HTML-encoding APIs; it has no separate template engine dependency.
- [Markdig](https://github.com/xoofx/markdig) renders Markdown (CommonMark) files as HTML.
- Search creates a static JSON index at render time; the browser searches it locally without a search service.
- Icons come from Iconify JSON packages that are embedded in `StaticCode.Icons` and rendered as inline SVG, so pages need no icon font or CDN.
- `StaticCode.Diagrams` draws the supported Mermaid and PlantUML syntax as SVG during rendering. The build does not need Mermaid JavaScript or a PlantUML/Java installation; `context` diagrams are a MarkdownUtil extension.
