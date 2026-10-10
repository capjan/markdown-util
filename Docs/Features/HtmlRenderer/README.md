# HTML Renderer

[Dokumentation](../../README.md) • [Features](../README.md) • HTML Renderer

## HTML Renderer Features / Goals

- Light and dark themes with an OS-following Auto setting and a collapsible, responsive sitemap navigation
- Responsive layouts (mobile first)
- Syntax highlighting of code blocks
- Automatic Asset copying to the output
 
## Technical Facts

- The embedded HTML template is rendered by `HtmlRenderer` using .NET string and HTML-encoding APIs; it has no separate template-engine dependency
- [Markdig](https://github.com/xoofx/markdig) renders the Markdown (CommonMark) files to HTML
