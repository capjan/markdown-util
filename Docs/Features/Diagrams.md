# Diagrams

## Overview

MarkdownUtil draws diagrams from fenced code blocks while it renders your Markdown. It understands a subset of the Mermaid and PlantUML syntax and turns each diagram into static SVG that is embedded directly in the generated HTML page.

> [!IMPORTANT]
> MarkdownUtil does not contain Mermaid or PlantUML, and it does not call them. It only imitates part of their syntax and draws the diagrams with its own implementation. The result therefore looks different from the originals, and not every feature is available.

### Static rendering

Every diagram is finished when `mdu render` or `mdu serve` writes the page:

- The page contains plain SVG. No JavaScript runs in the browser, and no diagram service is contacted.
- Diagrams appear immediately and also work offline.
- The colors come from the page's stylesheet, so diagrams follow the light and dark theme automatically.
- Diagram nodes can link to another page. The node's shape and label are clickable in HTML output.

### Portability

Your diagrams stay in the standard notation, so you can leave MarkdownUtil at any time without rewriting them:

- Mermaid diagrams stay ordinary ` ```mermaid ` code blocks. GitHub and other Mermaid-aware tools render the same files with the real Mermaid.
- PlantUML diagrams stay ordinary ` ```plantuml ` code blocks that PlantUML itself and PlantUML-aware tools render.
- Context diagrams are the exception: they are a MarkdownUtil extension in ` ```context ` code blocks, which other tools show as plain code.

Mermaid flowcharts support subgraphs, and MarkdownUtil also renders the class diagram subset described below. Syntax outside the documented subsets remains a code block instead of being guessed; `mdu lint` reports unsupported Mermaid syntax.

## Flowcharts

~~~markdown title="Markdown"
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

### Supported flowchart syntax

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
| Node links | `click A href "details.md"` |
| Longer links | Add dashes, dots, or equal signs, for example `--->` |
| Chains and groups | `A --> B --> C`, `A & B --> C` |
| Subgraphs | `subgraph Id["Label"]` … `end` |
| Line breaks | `<br>` inside a label |
| Comments | Lines starting with `%%` |

Write a link label between pipes directly after the link, for example `A -->|Text| B`. Quote labels that contain brackets: `A["Text (with brackets)"]`.

Make a node clickable with a `click` statement. Its destination can be a relative Markdown page or an external URL:

~~~markdown title="Markdown"
```mermaid
flowchart LR
    Store[Online Store]
    click Store href "store.md" "Open the store details"
```
~~~

```mermaid
flowchart LR
    Store[Online Store]
    click Store href "store.md" "Open the store details"
```

Relative `.md` destinations are changed to `.html` in the rendered site. `mdu lint` checks these destinations for matching path casing and existing pages, just like Markdown links. The optional tooltip is ignored.

Styling statements such as `style`, `classDef`, `class`, and `linkStyle` are ignored, because the page theme defines the colors. `accTitle: Text` sets the accessible name of the diagram.

## Sequence diagrams

~~~markdown title="Markdown"
```mermaid
sequenceDiagram
    actor User
    participant Web as Web app
    participant API
    User->>Web: Open dashboard
    Web->>+API: GET /dashboard
    alt Cached
        API-->>Web: Cached widgets
    else Not cached
        API->>API: Load widgets
        API-->>Web: Fresh widgets
    end
    deactivate API
    Note over Web,API: All calls use TLS
    Web-->>User: Render page
```
~~~

```mermaid
sequenceDiagram
    actor User
    participant Web as Web app
    participant API
    User->>Web: Open dashboard
    Web->>+API: GET /dashboard
    alt Cached
        API-->>Web: Cached widgets
    else Not cached
        API->>API: Load widgets
        API-->>Web: Fresh widgets
    end
    deactivate API
    Note over Web,API: All calls use TLS
    Web-->>User: Render page
```

### Supported sequence diagram syntax

| Element | Syntax |
| --- | --- |
| Header | `sequenceDiagram` |
| Participants | `participant A`, `participant A as Label`, `actor A as Label` |
| Messages | `->`, `-->`, `->>`, `-->>`, `-x`, `--x`, `-)`, `--)`, `<<->>`, `<<-->>`, each followed by `: Text` |
| Activations | `activate A`, `deactivate A`, or `+` and `-` after the arrow, for example `A->>+B: Text` |
| Notes | `Note left of A: Text`, `Note right of A: Text`, `Note over A: Text`, `Note over A,B: Text` |
| Blocks | `loop`, `alt` with `else`, `opt`, `par` with `and`, `critical` with `option`, `break`, each closed with `end` |
| Highlighted areas | `rect` … `end`; the color argument is ignored |
| Numbering | `autonumber`, `autonumber 10 5` (start and step), `autonumber off` |
| Titles | `title Text`, `accTitle: Text` |
| Line breaks | `<br>` inside a label, message, or note |
| Comments | Lines starting with `%%` |

Participants appear in the order they are declared or first used. `box`, `create`, `destroy`, and participant links are not supported yet.

## Class diagrams

Use `classDiagram` or `classDiagram-v2` for a class diagram. Class names are shown as written. Add relationships between classes with an optional label:

```mermaid
classDiagram
class Animal
Animal <|-- Duck
Duck *-- Wing : has
Bird ..> Nest : uses
```

The supported relationships are inheritance (`<|--`, `--|>`), composition (`*--`, `--*`), aggregation (`o--`, `--o`), dependency (`<..`, `..>`), and solid or dotted associations (`--`, `-->`, `<--`, `..`, `<|..`, `..|>`). Class declarations use `class Name`. Attributes, methods, aliases, and display labels are not supported.

## PlantUML component diagrams

~~~markdown title="Markdown"
```plantuml
@startuml
actor Customer
package "Shop" {
  [Web Shop] <<frontend>> as Web
  component [Order Service] as Orders
}
database "Orders DB" as DB
queue "Events" as Bus
Customer --> Web : browses
Web --> Orders : REST
Orders --> DB
Orders -right-> Bus : publishes
note right of DB : PostgreSQL
@enduml
```
~~~

```plantuml
@startuml
actor Customer
package "Shop" {
  [Web Shop] <<frontend>> as Web
  component [Order Service] as Orders
}
database "Orders DB" as DB
queue "Events" as Bus
Customer --> Web : browses
Web --> Orders : REST
Orders --> DB
Orders -right-> Bus : publishes
note right of DB : PostgreSQL
@enduml
```

### Supported PlantUML syntax

MarkdownUtil follows PlantUML's component diagram syntax, so the same source works with PlantUML itself:

| Element | Syntax |
| --- | --- |
| Frame | `@startuml` and `@enduml` are optional |
| Components | `[Name]`, `[Name] as Alias`, `component Name`, `component "Name" as Alias`, `component Alias as "Name"` |
| Interfaces | `() "Name"`, `() "Name" as Alias`, `interface Name`; an unknown name in a link also becomes an interface |
| Other elements | `actor`, `database`, `node`, `cloud`, `folder`, `frame`, `package`, `rectangle`, `card`, `artifact`, `file`, `queue`, `storage` |
| Containers | Any of `package`, `node`, `folder`, `frame`, `cloud`, `database`, `rectangle`, or `component` followed by `{` … `}`, also nested and without a name |
| Stereotypes | `<<name>>` after the element |
| Links | `--` solid, `..` dotted, `==` bold, with `>` or `<` for arrow ends, for example `-->` or `<..`; triangle ends are described below |
| Link direction | `->` keeps both elements in one row, `-->` puts the target below, more dashes make the link longer; `-up->`, `-down->`, `-left->`, `-right->` (also `-u->` and so on) |
| Link styles | `-[hidden]->` only affects the layout, `-[dashed]->`, `-[bold]->` |
| Link labels | `A --> B : Text` |
| Node links | `url of ID is [[details.md]]` |
| Notes | `note left of A : Text`, `note right of`, `note top of`, `note bottom of`, multi-line notes ending with `end note`, `note "Text" as N` |
| Layout | `left to right direction`, `top to bottom direction` |
| Text | `title Text`, `\n` for a line break |
| Comments | Lines starting with `'` and `/'` … `'/` blocks |

A triangle end, as for realization or generalization, is written `..|>` or `<|--`.

Link a node to another page with PlantUML's URL statement. `ID` is the element's alias when it has one:

~~~markdown title="Markdown"
```plantuml
@startuml
component [Order Service] as Orders
url of Orders is [[order-service.md]]
@enduml
```
~~~

```plantuml
@startuml
component [Order Service] as Orders
url of Orders is [[order-service.md]]
@enduml
```

Relative `.md` destinations are changed to `.html` in the rendered site. `mdu lint` checks these destinations for matching path casing and existing pages, just like Markdown links.

`skinparam`, `hide`, `show`, and colors are ignored, because the page theme defines the look. Other PlantUML diagram types, such as sequence or class diagrams, stay code blocks.

## Context diagrams

A context diagram shows one system in the center and the people and systems it works with around it.

> [!NOTE]
> Context diagrams are a MarkdownUtil extension. Neither Mermaid nor PlantUML has this diagram type, so only `mdu` renders them. GitHub and other tools show the ` ```context ` block as plain code.

A context diagram uses the flowchart syntax for systems and links, and `actor` for people:

~~~markdown title="Markdown"
```context
Store[Online Store]
Store <--> IdP[Identity Provider<br>OIDC]
Store <--> Payment[Payment Provider]
Store --> Shipping[Shipping Service]
Store <--> Inventory[Inventory and Order System]
Store --> Email[Email Service]
actor Customer
actor Manager as Store Manager
actor Warehouse as Warehouse Team
Customer -. Browses and orders .-> Store
Manager -. Manages catalog .-> Store
Warehouse -. Handles shipments .-> Store
```
~~~

```context
Store[Online Store]
Store <--> IdP[Identity Provider<br>OIDC]
Store <--> Payment[Payment Provider]
Store --> Shipping[Shipping Service]
Store <--> Inventory[Inventory and Order System]
Store --> Email[Email Service]
actor Customer
actor Manager as Store Manager
actor Warehouse as Warehouse Team
Customer -. Browses and orders .-> Store
Manager -. Manages catalog .-> Store
Warehouse -. Handles shipments .-> Store
```

### Supported context diagram syntax

| Element | Syntax |
| --- | --- |
| Systems | The flowchart shapes, for example `A[Text]`, `A(Text)`, `A([Text])`, `A((Text))`, `A{Text}` |
| People | `actor A` or `actor A as Label` |
| Links | The flowchart links, link ends, and labels, for example `-->`, `<-->`, `-.->`, `==>`, and `A -- Text --> B` |
| Node links | `click A href "details.md"` |
| Titles | `accTitle: Text` |
| Comments | Lines starting with `%%` |

The system in the center is the one that every link connects to. If several systems qualify, for example with a single link, the first one in the source is used. All other systems and people are spread evenly around the center, clockwise from the top in the order they first appear, so you control the arrangement by reordering the lines. A context diagram needs no header line.

Use a Mermaid-style `click` statement to link a system or person to its detail page:

~~~markdown title="Markdown"
```context
Store[Online Store]
Store --> Inventory[Inventory System]
click Inventory href "inventory.md"
```
~~~

```context
Store[Online Store]
Store --> Inventory[Inventory System]
click Inventory href "inventory.md"
```

Relative `.md` destinations are changed to `.html` in the rendered site. `mdu lint` checks these destinations for matching path casing and existing pages, just like Markdown links.
