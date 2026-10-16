# Diagrams

## Overview

MarkdownUtil renders Mermaid flowcharts to static SVG while it renders your Markdown. The diagrams need no JavaScript, appear immediately, and follow the page's light or dark theme because their colors come from the stylesheet instead of the diagram.

The source stays an ordinary ` ```mermaid ` code block, so the same file still shows its diagrams on GitHub and in other Mermaid-aware tools.

## Example

~~~markdown
```mermaid
flowchart TD
    Start([Open a Markdown file]) --> Lint{Lint passes?}
    Lint -->|Yes| Render[Render HTML]
    Lint -->|No| Fix[Fix the reported issues]
    Fix --> Lint
    Render --> Publish((Publish))
```
~~~

```mermaid
flowchart TD
    Start([Open a Markdown file]) --> Lint{Lint passes?}
    Lint -->|Yes| Render[Render HTML]
    Lint -->|No| Fix[Fix the reported issues]
    Fix --> Lint
    Render --> Publish((Publish))
```

Use `LR` for a diagram that flows from left to right:

```mermaid
flowchart LR
    Docs[Docs folder] --> Render[mdu render] --> Site[Static site]
    Render -.-> Search[Search index]
```

## Supported syntax

MarkdownUtil supports this subset of Mermaid's flowchart syntax:

| Element | Syntax |
| --- | --- |
| Header | `flowchart` or `graph`, optionally followed by `TD`, `TB`, `BT`, `LR`, or `RL` |
| Rectangle | `A[Text]` |
| Rounded rectangle | `A(Text)` |
| Stadium | `A([Text])` |
| Circle | `A((Text))` |
| Decision | `A{Text}` |
| Links | `-->`, `---`, `-.->`, `-.-`, `==>`, `===` |
| Link ends | `<-->`, `--o`, `--x` |
| Link labels | `A -- Text --> B`, or the text between two pipes after the link (see below) |
| Longer links | Add dashes, dots, or equal signs, for example `--->` |
| Chains and groups | `A --> B --> C`, `A & B --> C` |
| Line breaks | `<br>` inside a label |
| Comments | Lines starting with `%%` |

Write a link label between pipes directly after the link, for example `A -->|Text| B`. Quote labels that contain brackets: `A["Text (with brackets)"]`.

Styling statements such as `style`, `classDef`, `class`, and `linkStyle` are ignored, because the page theme defines the colors. `accTitle: Text` sets the accessible name of the diagram.

If a diagram uses syntax outside this subset, such as `subgraph` or other diagram types, MarkdownUtil shows its source as a code block instead.
