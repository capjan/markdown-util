# OpenAPI Spec

MDU turns a local OpenAPI specification into a static API reference inside a Markdown page. The specification stays in its JSON or YAML file; MDU adds the generated reference when it renders the page.

For a complete short example with the generated output, see [OpenAPI Spec example](openapi-example.md).

## Add an API reference

Set `openapi` in the page's front matter. Its path is relative to the Markdown file:

```markdown
---
title: Orders API
description: Reference for the Orders service.
openapi: ./orders.yaml
---

# Orders API

This introduction appears before the generated reference.
```

`mdu render`, `mdu serve`, and `mdu pdf` support `.json`, `.yaml`, and `.yml` specifications. The reference follows the page's Markdown content. `mdu serve` also watches the folders containing referenced specifications and refreshes the page when a specification changes.

## Generated reference

Operations are grouped by their first tag. Each operation shows its method and path, parameters, request body, responses, content types, and examples. Schemas defined in the same specification appear in a separate section.

The generated reference gives each HTTP method a distinct label, highlights endpoint paths, and separates API groups and schemas. It follows the site's light or dark theme and is published as static HTML and CSS without making API requests. References within the specification are resolved; external `$ref` files are not loaded.
