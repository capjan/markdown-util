---
navigation.order: 4
---

# render

`mdu render` converts the detected Markdown pages into a static HTML site. It writes the result and copied local assets to the output folder, which defaults to `./site`. The command exits after rendering.

## Usage

```bash
mdu render [PATH [OUTPATH]]
```

The alias `mdu r` runs the same command. See [Paths and file search](README.md#paths-and-file-search) for the default source and output paths, and [common options](README.md#common-options) for `--pattern` and `--hidden`. Run `mdu render --help` for the current command help.

`--editPageRoot` adds an edit icon beside the first level-one heading on each rendered page. Pages without a level-one heading show an **Edit this page** link in the footer. It also adds a GitHub repository button to the top bar; the button links to the repository root and shows its `owner/repository` name.

## Site configuration

An optional `mdu.yml` file in the Markdown root configures the site name, light and dark logos and favicons, and theme colors. The configured background color also applies to the header, navigation, sidebar, and dialogs. Secondary text, lines, and code surfaces are mixed from the configured text and background colors, and hover backgrounds from the primary color, so they match a custom palette. Referenced assets are copied into the output folder.

```yaml
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

Page front matter supports `title`, `description`, and `icon`. `title` sets the browser and fallback navigation title, `description` becomes the page meta description, and `icon` adds an icon shortcode beside the first-level heading. `navigation.title` overrides the navigation label; `navigation.order`, `navigation.visible`, and `navigation.expanded` control ordering, visibility, and initial sidebar expansion. Set `navigation.placement: top` on a first-level page or section `README.md` to create a [top navigation tab](../Features/Navigation.md#top-navigation). The placement chooses the preferred position for that entry; it does not change the navigation or breadcrumb root.

## Examples

Render the default documentation folder to `./site`, or choose both folders explicitly:

```bash
mdu render
mdu render ./ProjectDocs ./public/docs
```

Render only files matching a custom pattern and add links to their source files in a GitHub repository:

```bash
mdu render ./Docs ./site --pattern "*.markdown" --editPageRoot "https://github.com/example/project/edit/main/Docs"
```

The Markdown source files are left unchanged.
