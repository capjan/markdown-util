---
navigation.order: 4
---

# render

`mdu render` converts the Markdown pages into a static HTML site and exits. It writes the pages and the linked local assets to the output folder, `./site` by default. Your Markdown files stay unchanged.

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

An optional `mdu.yml` in the documentation folder sets the site name, light and dark logos and favicons, and theme colors. MDU copies the referenced files into the output folder.

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
