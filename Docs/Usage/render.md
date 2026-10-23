---
navigation.order: 4
---

# render

`mdu render` converts the Markdown pages into a static HTML site and exits. It writes the pages and linked local assets to the output folder, `./site` by default. If any page contains `[Download the PDF](mdu:pdf)`, MDU also generates a combined `documentation.pdf` there and resolves the link to that file. It renders like any other link. Your Markdown files stay unchanged.

## Usage

```bash
mdu render [PATH [OUTPATH]] [options]
```

The alias `mdu r` runs the same command. See [Paths and file search](README.md#paths-and-file-search) for the default folders and [common options](README.md#common-options) for `--pattern` and `--hidden`.

| Option | Effect |
| --- | --- |
| `--editPageRoot <url>` | GitHub edit URL of the documentation folder. Adds an edit icon beside each page's level-one heading, or an **Edit this page** link in the footer when there is none, and a repository button with the `owner/repository` name to the header. |

## Examples

```bash
mdu render
mdu render ./ProjectDocs ./public/docs
mdu render ./docs ./site --pattern "*.markdown" --editPageRoot "https://github.com/example/project/edit/main/docs"
```

## Site configuration

An optional `mdu.yml` in the documentation folder sets the site name, light and dark logos, and theme colors. If no favicon files are specified, MDU uses the light and dark logo files as favicons. MDU copies the referenced files into the output folder. A square logo mark is most legible in a browser tab; MDU uses the logo file as-is.

An optional `favicon:` value can be a single asset path for both themes, or a mapping with separate `light` and `dark` paths. MDU emits separate light and dark favicon links, and the browser selects between them based on its color scheme, independently of the page theme. A single SVG that uses `prefers-color-scheme` internally is the most robust option because it adapts to the browser's color scheme itself. Safari 26.3 had a reported issue where SVG favicons ignored this media query ([WebKit issue](https://bugs.webkit.org/show_bug.cgi?id=309949)); Safari 27 lists a related SVG color-scheme fix. Older Safari versions before Safari 26 do not display SVG favicons, so use PNG or ICO if those versions must be supported.

```yaml title="docs/mdu.yml"
site_name: Documentation
logo:
  light: assets/logo-light.svg
  dark: assets/logo-dark.svg
favicon:
  light: assets/favicon-light.svg
  dark: assets/favicon-dark.svg
colors:
  light:
    background: "#ffffff"
    text: "#1d1d1f"
    primary: "#0066cc"
  dark:
    background: "#1d1d1f"
    text: "#f5f5f7"
    primary: "#2997ff"
```

The background color also applies to the header, navigation, sidebar, and dialogs. Secondary text, lines, and code surfaces are mixed from the text and background colors, and hover backgrounds from the primary color, so they match a custom palette.

## Page front matter

YAML front matter at the top of a page controls its title, metadata, and navigation entry:

```yaml
---
title: Installing MDU
description: Install the MDU .NET tool globally or per repository.
icon: material-download
navigation:
  title: Setup
  order: 10
---
```

| Key | Effect |
| --- | --- |
| `title` | Browser title and fallback navigation title |
| `description` | Page meta description |
| `icon` | Icon shortcode shown beside the level-one heading, such as `material-download` |
| `navigation.title` | Label in the sidebar, tabs, and breadcrumbs |
| `navigation.order` | Sort order among sibling pages |
| `navigation.visible` | `false` hides the entry; the page stays reachable by its URL |
| `navigation.expanded` | `true` opens the section in the sidebar initially |
| `navigation.placement` | `top` makes a first-level page or section a [top navigation tab](../Features/Navigation.md#top-navigation) |
| `pdf.visible` | `false` leaves the page out of [`mdu pdf`](pdf.md) |

Both the nested form shown above and the compact form `navigation.order: 10` work. [Navigation](../Features/Navigation.md) explains the navigation keys in detail.
