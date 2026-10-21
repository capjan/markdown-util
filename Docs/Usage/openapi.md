---
navigation.order: 5
---

# openapi render

`mdu openapi render` turns one local OpenAPI document into a static HTML site using MDU's page layout and stylesheet. The output can be hosted as ordinary files and does not send API requests.

## Usage

```bash
mdu openapi render <SPEC> [OUTPATH]
```

`SPEC` must be a local `.json`, `.yaml`, or `.yml` OpenAPI document. `OUTPATH` is the output folder and defaults to `./openapi-site`.

```bash
mdu openapi render ./api/orders.yaml
mdu openapi render ./api/orders.yaml ./public/orders-api
```

The output folder contains `index.html`, MDU's stylesheet and the small local search assets. For a reference embedded in a larger documentation site, use the [`openapi` front matter key](render.md#openapi-references) with `mdu render`, `mdu serve`, or `mdu pdf` instead.
