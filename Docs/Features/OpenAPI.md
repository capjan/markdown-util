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

Operations are grouped by their first tag in a compact list. Tag groups and endpoints start collapsed; expand a tag to browse its endpoints, then expand an endpoint to inspect its method and path, parameters, request body, and responses. The search field above the tags filters endpoint paths without regard to case. Nonmatching endpoints and empty tags disappear; clearing the search restores the full list and the tags' previous expanded state.

The response selector shows the status code and name, followed by a media type selector when the response has content. Responses and request bodies provide Example, Schema, and Schema JSON tabs. Schema descriptions appear below their names, and referenced schemas are linked and displayed inline. Response and media type selectors show their position as `1 of N`. 2xx statuses are green, 4xx are yellow, and 5xx are red.

The generated reference gives each HTTP method a distinct label, highlights endpoint paths, and separates API groups and schemas. It follows the site's light or dark theme and is published as static HTML and CSS without making API requests. References within the specification are resolved; external `$ref` files are not loaded.
