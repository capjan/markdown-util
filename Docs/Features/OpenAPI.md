# OpenAPI Spec

Embed interactive static API references anywhere in a Markdown page with an `openapi` code block. MDU supports OpenAPI 3.1, 3.0, and 2.0 specifications in `.yaml`, `.yml`, and `.json`.

## Reference a specification file

Point an `openapi` code block at a local specification file (relative to the Markdown page):

````markdown title="Markdown"
```openapi
./orbital-spaceport.yaml
```
````

MDU renders the interactive reference in place and generates a static **OpenAPI specification** page (linked right in the overview below) with syntax highlighting and a copy button:

```openapi
./orbital-spaceport.yaml
```

## Inline specification

You can also write an OpenAPI specification directly inside an `openapi` code block — and place multiple specifications on the same page:

````markdown title="Markdown"
```openapi
openapi: 3.1.0
info:
  title: Solar Weather Beacon API
  version: 1.0.0
  description: Real-time solar flare alerts for orbital crew safety.
paths:
  /alerts/current:
    get:
      tags: [Solar Weather]
      summary: Get current solar storm index
      responses:
        '200':
          description: Active geomagnetic and X-ray flux reading.
          content:
            application/json:
              schema:
                type: object
                required: [kpIndex, stormLevel, updatedAt]
                properties:
                  kpIndex: { type: integer, minimum: 0, maximum: 9, default: 2 }
                  stormLevel: { type: string, enum: [quiet, moderate, severe, extreme] }
                  updatedAt: { type: string, format: date-time, readOnly: true }
```
````

Rendered output:

```openapi
openapi: 3.1.0
info:
  title: Solar Weather Beacon API
  version: 1.0.0
  description: Real-time solar flare alerts for orbital crew safety.
paths:
  /alerts/current:
    get:
      tags: [Solar Weather]
      summary: Get current solar storm index
      responses:
        '200':
          description: Active geomagnetic and X-ray flux reading.
          content:
            application/json:
              schema:
                type: object
                required: [kpIndex, stormLevel, updatedAt]
                properties:
                  kpIndex: { type: integer, minimum: 0, maximum: 9, default: 2 }
                  stormLevel: { type: string, enum: [quiet, moderate, severe, extreme] }
                  updatedAt: { type: string, format: date-time, readOnly: true }
```

## Highlights

| Area | Behavior |
| --- | --- |
| **Overview & spec page** | Version, license, contact, server URLs, security schemes, and an automatic **OpenAPI specification** link to a static page with the full syntax-highlighted source. |
| **Tags & filter** | Collapsible tag groups with HTTP method counters, instant path search, auth locks with OAuth scopes, and a **Show deprecated** switch. |
| **Requests & responses** | Status-colored response picker (`2xx` green, `4xx` yellow, `5xx` red), response headers, media-type switcher, and **Example**, **Schema**, and **Schema JSON** tabs. |
| **Schemas & examples** | Compact `name: Type` rows with required markers (`*`), inline enums, maps (`[string: Type]`), bounds, defaults, collapsible descriptions, and auto-generated examples respecting `readOnly` / `writeOnly`. |
| **Tooling** | `mdu serve` watches referenced spec files for live reload, `mdu page move` updates relative paths in `openapi` blocks, and `mdu pdf` includes the reference in the PDF export. |

> [!NOTE]
> Local specification files and internal `$ref` pointers (`#/...`) are resolved and linked inline; external `$ref` files and remote `http(s)` specification URLs are skipped so builds stay fast and reproducible offline.
