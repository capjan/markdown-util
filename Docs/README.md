# Documentation

MarkdownUtil is a .NET 10 tool for writing documentation in ordinary Markdown and publishing it as a static HTML site. It brings linting, local preview, and rendering into one CLI, while the folder structure supplies the site navigation.

## Why MarkdownUtil?

- **Use one .NET tool for the docs workflow.** Lint pages, preview changes, and render the site without a separate Node.js or Python build toolchain.
- **Turn folders into a navigable site.** MarkdownUtil builds the sidebar and breadcrumbs from `README.md` pages and their folder structure.
- **Find link issues before publishing.** The linter checks local links, images, and path casing, and reports the source file and line.
- **Keep your Markdown and choose your host.** The sources remain readable Markdown, and the rendered output is static HTML. Optional `sidebar.order` metadata is specific to MarkdownUtil.

## Contents

- [Setup](./Setup/README.md) - How to install and set up the tool
- [Usage](./Usage/README.md) - How to use the tool
- [Features](./Features/README.md) - An overview of the features
- [Architecture](./Architecture/README.md) - How MarkdownUtil turns Markdown into a rendered site
- [Other Tools](./OtherTools/README.md) - More established tools with similar goals
