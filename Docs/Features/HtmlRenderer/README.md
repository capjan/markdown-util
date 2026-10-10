# HTML Renderer

[Documentation](../../README.md) • [Features](../README.md) • HTML Renderer

## Features and goals

- Light and dark themes, with an automatic mode that follows the operating system.
- A collapsible, responsive sitemap navigation.
- Responsive, mobile-first layouts.
- Syntax highlighting for code blocks.
- Automatic copying of assets to the output directory.
 
## Technical details

- The embedded HTML template is rendered by `HtmlRenderer` using .NET string and HTML-encoding APIs; it has no separate template engine dependency.
- [Markdig](https://github.com/xoofx/markdig) renders Markdown (CommonMark) files as HTML.
