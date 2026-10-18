# MarkdownUtil

[![.NET 10](https://github.com/capjan/markdown-util/actions/workflows/dotnet.yml/badge.svg)](https://github.com/capjan/markdown-util/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/MarkdownUtil)](https://www.nuget.org/packages/MarkdownUtil)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://opensource.org/licenses/MIT)

**Turn a folder of Markdown files into a documentation website. One command, zero configuration.**

[![A page rendered by MarkdownUtil in light and dark theme](https://raw.githubusercontent.com/capjan/markdown-util/main/.github/assets/screenshot.png)](https://capjan.github.io/markdown-util/)

Point `mdu` at your `docs` folder and get a clean static HTML site with sidebar navigation, breadcrumbs, client-side search, dark mode, and syntax highlighting. Search results jump to matching sections and highlight the found text. There is no config file to write, no theme to set up, and no Node.js or Python toolchain to install. Your files stay plain Markdown that still reads well on GitHub.

👉 **[See a live example](https://capjan.github.io/markdown-util/)**: the project documentation, rendered by `mdu` itself.

## From Markdown to website in a minute

**1. Install** (requires the [.NET 10 SDK](https://dotnet.microsoft.com/download)):

```bash
dotnet tool install --global MarkdownUtil
```

**2. Write your docs** in a `docs` folder. Each folder gets a `README.md` as its page:

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

Your site opens at <http://localhost:4173> and refreshes whenever you save a file.

**4. Publish it:**

```bash
mdu render
```

Upload the generated `site` folder to GitHub Pages, GitLab Pages, Netlify, or any plain web server. It's just static HTML.

## What you get

- 🧭 **Navigation for free.** The sidebar and breadcrumbs follow your folder structure. Order pages with an optional `sidebar.order` in the front matter.
- 🌗 **Light and dark theme** that follows the visitor's system setting.
- 🔎 **Fast client-side search** with section links and highlighted matches.
- 📱 **Responsive layout** that works on phones, tablets, and desktops.
- 🎨 **Syntax highlighting** for code blocks.
- 🖼️ **Assets included.** Linked images and files are copied to the site automatically.
- ✅ **Built-in linter.** `mdu lint` catches broken links, wrong link casing, and missing titles before you publish.

## Why MarkdownUtil?

- **One .NET workflow for your docs.** `mdu lint`, `mdu serve`, and `mdu render` run from the .NET 10 tool; there is no separate Node.js or Python build toolchain.
- **Your folder structure becomes navigation.** `README.md` files represent sections, and MarkdownUtil builds the sidebar and breadcrumbs from the folder tree. Set `sidebar.order` only when you want a custom order.
- **Catch problems before publishing.** The linter checks page titles, directory names, link casing, and missing local links or images, then points to the source file and line.
- **Keep your content and hosting portable.** Your source stays Markdown and the generated site is static HTML you can host anywhere. The optional `sidebar.order` field is MarkdownUtil-specific.

## Commands

| Command           | What it does                                       |
| ----------------- | -------------------------------------------------- |
| `mdu serve`       | Renders, serves, and live-reloads the site.        |
| `mdu render`      | Writes the static site to `./site`.                |
| `mdu lint`        | Checks the docs and reports problems.              |
| `mdu fix`         | Applies automatic fixes for supported warnings.    |
| `mdu tree`        | Prints Markdown files under `./docs` or `./Docs` as a tree. |

The defaults (`./docs` in, `./site` out, port `4173`) cover the common case. Pass paths or options only when your layout differs:

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

- [Documentation](https://capjan.github.io/markdown-util/)
- [Usage](https://github.com/capjan/markdown-util/blob/main/Docs/Usage/README.md), [linter rules](https://github.com/capjan/markdown-util/blob/main/Docs/Usage/lint/README.md), and [sidebar ordering](https://github.com/capjan/markdown-util/blob/main/Docs/Features/Sidebar/README.md)

> **Project status:** MarkdownUtil is under active development. Expect changes before the first stable release.
