# Hero Banner

## Overview

A hero banner opens a landing page with an animated scene: waves roll in, break into whitewater, and wash up the beach while your title and calls to action sit on top. The scene is rendered live in the browser with WebGL, follows the light and dark theme, and needs no images, fonts, or external scripts.

```hero
title: Hero banner
text: This banner uses the content width. Switch the theme to see the scene change from day to night.
width: content
height: 380
actions:
  - text: See the options
    link: "#options"
```

## Syntax

Add a fenced code block with the language `hero` and describe the banner with YAML. Place it right below the page heading:

````markdown title="Markdown"
# Documentation

```hero
title: Plain Markdown, beautifully published
text: Lint, inspect, and render your Markdown files into a fast static documentation site.
actions:
  - text: Get started
    link: Setup.md
  - text: Browse features
    link: ./Features/README.md
```
````

The source stays an ordinary code block, so the file remains readable on GitHub and in any other Markdown viewer.

## Options

| Option | Values | Default | Description |
|---|---|---|---|
| `title` | Text | | Large heading of the banner. |
| `text` | Text | | Short introduction below the title. |
| `actions` | List | | Buttons with `text`, `link`, and an optional `style` of `primary` or `secondary`. The first button is primary unless you set a style. Links to `.md` files point to the rendered pages, and links to other sites open in a new tab. |
| `time` | `auto`, `day`, `sunset`, `night` | `auto` | Lighting of the scene. `auto` shows the day in the light theme and a moonlit night in the dark theme, and fades between them when the reader switches the theme. |
| `width` | `full`, `content` | `full` | `full` spans the whole window below the header, `content` places a rounded banner in the page text. |
| `align` | `left`, `center` | `left` | Position of the title, text, and buttons. |
| `height` | 240 to 1200 | `520` | Height in pixels. The banner never takes more than 85 % of the window height. |
| `waves` | 0 to 2.5 | `1` | Wave height, from a calm sea at `0` to heavy surf. |
| `speed` | 0 to 4 | `1` | Animation speed. `0` freezes the scene. |
| `quality` | `auto`, `low`, `high` | `auto` | Render resolution. `auto` lowers the resolution on devices that cannot keep up. |
| `colors` | `water`, `shallow`, `sand` | | Hex colors such as `"#0a3d62"` that replace the colors of deep water, shallow water, and sand. Quote them, because `#` starts a YAML comment. |

Example with every option:

````markdown title="Markdown"
```hero
title: Release 3.0
text: Faster rendering, new diagrams, and a fresh look.
time: sunset
width: full
align: center
height: 600
waves: 1.4
speed: 0.8
quality: auto
actions:
  - text: Read the changelog
    link: Changelog.md
  - text: Source code
    link: https://github.com/capjan/mdu
    style: secondary
colors:
  water: "#0a3d62"
  shallow: "#1fa2a0"
  sand: "#e8d2a6"
```
````

## Behavior

- The animation only runs while the banner is visible and the browser tab is active.
- Readers who prefer reduced motion see a still image of the scene.
- Without WebGL, the banner shows a static gradient in the colors of the scene.
- A page shows at most one full-width banner; further `hero` blocks on the same page use the content width.
- The PDF export shows the title, text, and button labels as plain text.
