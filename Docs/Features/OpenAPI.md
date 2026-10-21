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

Operations are grouped by their first tag. Tag groups and endpoints start collapsed; expand a tag to browse its endpoints, then expand an endpoint to inspect its method and path, parameters, request body, and responses. The response selector shows the status code and name, followed by a media type selector when the response has content. A response description appears above tabs for its example, schema table, and JSON schema. Response and media type counts use singular or plural wording. 2xx statuses are green, 4xx are yellow, and 5xx are red. Schemas defined in the same specification appear in a separate section.

The generated reference gives each HTTP method a distinct label, highlights endpoint paths, and separates API groups and schemas. It follows the site's light or dark theme and is published as static HTML and CSS without making API requests. References within the specification are resolved; external `$ref` files are not loaded.
