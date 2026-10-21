# MDU

```hero
title: Plain Markdown, beautifully published
text: Lint, inspect, and render your Markdown files into a fast static documentation site – no lock-in, no build plugins.
time: auto
actions:
  - text: Get started
    link: Setup/README.md
  - text: Browse features
    link: ./Features/README.md
```

MDU is a .NET tool that turns a folder of ordinary Markdown files into a documentation site. Your pages stay plain Markdown that reads well on GitHub; MDU adds navigation, search, diagrams, and a polished theme when it renders them.

## Get started

- [Setup](Setup/README.md) - Choose a .NET tool or portable installation
- [Usage](./Usage/README.md) - Run the commands and preview your site
- [Publish](./Publish/README.md) - Host the generated site on GitHub, GitLab, or any web server

## Commands

| Command | Does |
| --- | --- |
| [mdu tree](./Usage/tree.md) | Shows the page structure |
| [mdu lint](./Usage/lint/README.md) | Finds broken links and structure problems |
| [mdu fix](./Usage/fix.md) | Migrates MkDocs syntax and applies automatic fixes |
| [mdu serve](./Usage/serve.md) | Previews the site with live reload |
| [mdu render](./Usage/render.md) | Generates the static site |
| [mdu pdf](./Usage/pdf.md) | Combines all pages into one PDF |
| [mdu page](./Usage/page.md) | Moves pages and updates every link to them |

## Write and organize content

- [Features](./Features/README.md) - Callouts, diagrams, math, icons, keyboard keys, and more
- [Navigation](./Features/Navigation.md) - Order pages, add top tabs, and hide entries
- [Repository Imports](./Features/Repository-Imports.md) - Combine Markdown pages from multiple GitHub repositories

## About MDU

- [Architecture](./Architecture/README.md) - How MDU reads, checks, and renders a documentation folder
- [Other tools](OtherTools.md) - Documentation site generators worth knowing
