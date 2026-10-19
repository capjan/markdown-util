# MDU

### Markdown Documentation Utility

[![.NET 10](https://github.com/capjan/mdu/actions/workflows/dotnet.yml/badge.svg)](https://github.com/capjan/mdu/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/MarkdownUtil)](https://www.nuget.org/packages/MarkdownUtil)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://opensource.org/licenses/MIT)

**Turn a folder of Markdown files into a documentation website. One command, zero configuration.**

[![MDU Callouts documentation in light and dark themes, split diagonally](https://raw.githubusercontent.com/capjan/mdu/main/.github/assets/screenshot.png)](https://capjan.github.io/mdu/)

MDU (`mdu`) turns a `docs` folder into a static HTML site with top navigation, a collapsible sidebar, breadcrumbs, client-side search, light and dark themes, and syntax highlighting. Search results jump to matching sections and highlight the found text. Add an animated hero banner or optional site configuration when you want to customize the site. Your files stay plain Markdown that still reads well on GitHub, and there is no Node.js or Python build toolchain to install.

👉 **[See a live example](https://capjan.github.io/mdu/)**: the MDU documentation, rendered by `mdu` itself.

## From Markdown to website in a minute

**1. Install the `MarkdownUtil` .NET tool package** (requires the [.NET 10 SDK](https://dotnet.microsoft.com/download)):

```bash
dotnet tool install --global MarkdownUtil
```

**2. Write your docs** in a `docs` folder. Each folder can use a `README.md` as its section page:

```text
docs/
├── README.md            → start page
├── Setup/
│   └── README.md        → "Setup" section
└── Usage/
    ├── README.md        → "Usage" section
    └── Configuration.md → page inside "Usage"
```

**3. Preview it** with live reload:

```bash
mdu serve
```

Your site opens at <http://localhost:4173> and refreshes when you save a Markdown file or linked local asset.

**4. Publish it:**

```bash
mdu render
```

Upload the generated `site` folder to GitHub Pages, GitLab Pages, Netlify, or any plain web server. It's just static HTML.

## What you get

- 🧭 **Navigation from your folders.** Top tabs, the sidebar, and breadcrumbs follow your folder structure. Use `navigation.*` front matter to order, label, or hide entries.
- 🌗 **Light and dark theme** that follows the visitor's system setting.
- 🔎 **Fast client-side search** with section links and highlighted matches.
- 📱 **Responsive layout** that works on phones, tablets, and desktops.
- 🎨 **Rich Markdown rendering.** Code highlighting, supported diagrams, math, icons, emoji, callouts, and an optional animated hero banner.
- 🖼️ **Assets included.** Linked images and files are copied to the site automatically.
- ✅ **Built-in linter.** `mdu lint` catches missing titles, broken local links and assets, and path casing issues before you publish.
- 📄 **PDF export.** Create one PDF from a documentation folder or a single Markdown file.

## Why MDU?

- **One .NET workflow for your docs.** `mdu tree`, `mdu lint`, `mdu fix`, `mdu serve`, `mdu render`, and `mdu pdf` run from the .NET 10 tool; there is no separate Node.js or Python build toolchain.
- **Your folder structure becomes navigation.** MDU builds tabs, the sidebar, and breadcrumbs from the folder tree. Use `navigation.order` when you want a custom order.
- **Catch problems before publishing.** The linter checks page titles, directory names, link casing, and missing local links or assets, then points to the source file and line.
- **Keep your content and hosting portable.** Your source stays Markdown and the generated site is static HTML you can host anywhere. The optional `navigation.*` front matter and `mdu.yml` site configuration are MDU-specific.

## Commands

| Command           | What it does                                       |
| ----------------- | -------------------------------------------------- |
| `mdu serve`       | Renders, serves, and live-reloads the site.        |
| `mdu render`      | Writes the static site to `./site`.                |
| `mdu pdf`         | Writes a documentation folder or one Markdown file to a PDF. |
| `mdu lint`        | Checks documentation and local links.              |
| `mdu fix`         | Migrates supported MkDocs Markdown and applies automatic fixes. |
| `mdu tree`        | Prints detected Markdown files as a tree.          |

By default, commands use the only `./docs` or `./Docs` folder in the current directory. Rendering writes to `./site`; PDF export writes to `./documentation.pdf` for a folder or a file named after a single Markdown input; and the preview server uses port `4173`. If neither documentation folder exists or both do, pass the source path explicitly:

```bash
mdu serve ./manual ./public --port 8080 --no-browser
```

Lint problems point straight to the file and line:

```text
/project/docs/Usage/README.md:7: MDU004 LocalLinkExists: Local link target './intro.md' does not exist.
Linter detected 1 error.
```

## Install per repository

To pin the tool to a repository instead of installing it globally:

```bash
dotnet new tool-manifest
dotnet tool install --local MarkdownUtil
```

Skip the first command if `.config/dotnet-tools.json` already exists. Run the local tool with `dotnet mdu`, for example `dotnet mdu serve`.

## Learn more

- [Documentation](https://capjan.github.io/mdu/)
- [Usage](https://github.com/capjan/mdu/blob/main/Docs/Usage/README.md), [linter rules](https://github.com/capjan/mdu/blob/main/Docs/Usage/lint/README.md), and [navigation settings](https://github.com/capjan/mdu/blob/main/Docs/Features/Navigation.md)
- [Features](https://github.com/capjan/mdu/blob/main/Docs/Features/README.md) and [PDF export](https://github.com/capjan/mdu/blob/main/Docs/Usage/pdf.md)
