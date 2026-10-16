# HTML Renderer

## Features and goals

- Light and dark themes, with an automatic mode that follows the operating system.
- A collapsible, responsive sitemap navigation.
- Responsive, mobile-first layouts.
- Syntax highlighting for code blocks.
- GitHub-compatible [admonitions](../../Features/Admonitions/README.md) for notes, tips, and warnings.
- Client-side search with direct links to matching sections and highlighted occurrences.
- Automatic copying of assets to the output directory.
- Breadcrumb navigation generated from the Markdown file structure.
 
## Technical details

- The embedded HTML template is rendered by `HtmlRenderer` using .NET string and HTML-encoding APIs; it has no separate template engine dependency.
- [Markdig](https://github.com/xoofx/markdig) renders Markdown (CommonMark) files as HTML.
- Search creates a static JSON index at render time; the browser searches it locally without a search service.
