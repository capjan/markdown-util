<p align="center">
  <a href="https://capjan.github.io/mdu/">
    <picture>
      <source media="(prefers-color-scheme: dark)" srcset="Docs/assets/mdu-logo-dark.svg">
      <source media="(prefers-color-scheme: light)" srcset="Docs/assets/mdu-logo-light.svg">
      <img alt="MDU" src="Docs/assets/mdu-logo-light.svg" width="96" height="96">
    </picture>
  </a>
</p>

<h1 align="center">MDU</h1>

<p align="center">
  <strong>Plain Markdown, beautifully published.</strong>
</p>

<p align="center">
  <a href="https://github.com/capjan/mdu/actions/workflows/dotnet.yml"><img
    src="https://github.com/capjan/mdu/actions/workflows/dotnet.yml/badge.svg"
    alt="Build"
  /></a>
  <a href="https://www.nuget.org/packages/mdu"><img
    src="https://img.shields.io/nuget/v/mdu?logo=nuget&logoColor=white&label=NuGet"
    alt="NuGet"
  /></a>
  <a href="https://hub.docker.com/r/capjan/mdu"><img
    src="https://img.shields.io/docker/pulls/capjan/mdu?logo=docker&logoColor=white&label=Docker"
    alt="Docker Pulls"
  /></a>
  <a href="https://github.com/capjan/mdu/releases/latest"><img
    src="https://img.shields.io/github/v/release/capjan/mdu?logo=github&label=Release"
    alt="Latest Release"
  /></a>
  <a href="LICENSE"><img
    src="https://img.shields.io/badge/license-MIT-blue"
    alt="License: MIT"
  /></a>
</p>

<p align="center">
  <a href="https://capjan.github.io/mdu/"><strong>Documentation</strong></a>
  &middot;
  <a href="https://capjan.github.io/mdu/Setup/"><strong>Get started</strong></a>
  &middot;
  <a href="https://capjan.github.io/mdu/Features/"><strong>Features</strong></a>
  &middot;
  <a href="https://capjan.github.io/mdu/Usage/"><strong>Commands</strong></a>
  &middot;
  <a href="https://capjan.github.io/mdu/Publish/"><strong>Publish</strong></a>
  &middot;
  <a href="https://github.com/capjan/mdu/releases"><strong>Releases</strong></a>
</p>

<p align="center">
  Turn a folder of ordinary Markdown files into a fast, searchable documentation
  site – with navigation, diagrams, a linter, and PDF export built in.
  One .NET tool, zero configuration, no Node.js or Python toolchain.
</p>

<p align="center">
  <a href="https://capjan.github.io/mdu/">
    <img alt="MDU documentation in light and dark themes" src=".github/assets/screenshot.png">
  </a>
</p>

<p align="center">
  <em>
    Visit the documentation at
    <a href="https://capjan.github.io/mdu/">capjan.github.io/mdu</a>
    – rendered by MDU itself.
  </em>
</p>

## Quick start

```bash
dotnet tool install --global mdu
mdu serve
```

MDU renders the `docs` folder and opens a live-reloading preview. Run `mdu render` to write the static site.
Prefer a standalone executable or Docker? See [Setup](https://capjan.github.io/mdu/Setup/).

## Documentation

| Section | What you will find |
| --- | --- |
| [**Setup**](https://capjan.github.io/mdu/Setup/) | Install MDU as a .NET tool, standalone executable, or Docker image. |
| [**Usage**](https://capjan.github.io/mdu/Usage/) | Preview, lint, render, and export your docs with the `mdu` commands. |
| [**Features**](https://capjan.github.io/mdu/Features/) | Callouts, diagrams, OpenAPI, math, icons, code highlighting, and more. |
| [**Navigation**](https://capjan.github.io/mdu/Features/Navigation.html) | Build tabs, sidebar, and breadcrumbs from your folder structure. |
| [**Publish**](https://capjan.github.io/mdu/Publish/) | Host the generated site on GitHub Pages, GitLab Pages, or any web server. |
| [**Architecture**](https://capjan.github.io/mdu/Architecture/) | Learn how MDU parses, validates, and renders your documentation. |

## License

MDU is released under the [MIT License](LICENSE).
