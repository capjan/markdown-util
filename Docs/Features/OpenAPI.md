# OpenAPI Spec

MDU turns a local OpenAPI specification into a static API reference inside a Markdown page. The specification stays in its JSON or YAML file; MDU adds the generated reference when it renders the page.

For a complete short example with the generated output, see [OpenAPI Spec example](openapi-example.md). The [Swagger Petstore example](openapi-petstore.md) shows a larger reference generated from the public Swagger Petstore specification.

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

`mdu render`, `mdu serve`, and `mdu pdf` support `.json`, `.yaml`, and `.yml` specifications in the OpenAPI 3 and Swagger 2.0 formats. The reference follows the page's Markdown content. `mdu serve` also watches the folders containing referenced specifications and refreshes the page when a specification changes.

## Generated reference

Operations are grouped by their first tag in a compact list. Tag groups and endpoints start collapsed; expand a tag to browse its endpoints, then expand an endpoint to inspect its method and path, parameters, request body, and responses. Each endpoint row shows the operation summary next to its path. The method counts beside a tag name the HTTP method on hover. The search field above the tags filters endpoint paths without regard to case. Nonmatching endpoints and empty tags disappear, and the method counts show only the matching endpoints; clearing the search restores the full list, the counts, and the tags' previous expanded state.

The response selector shows the status code and name, followed by a media type selector when the response has content. Responses and request bodies provide Example, Schema, and Schema JSON tabs. Schema descriptions appear below their names, and referenced schemas are linked and displayed inline. Response and media type selectors show their position as `1 of N`. 2xx statuses are green, 4xx are yellow, and 5xx are red.

Parameter and schema tables combine names and types as `name: Type`. Primitive types with a format show the format instead, such as `uuid`, `int64`, or `dateTime`. Required fields carry a red asterisk, and default values follow the type as `= value`. The allowed values of an enum appear below the field that uses it, so enums need no separate schema table. Table descriptions start hidden; use **Show descriptions** in the table header to reveal them. Tables without description text omit this control.

Generated examples use typical values for common string formats, such as a UUID or a UTC timestamp, when the specification provides no example.

The generated reference gives each HTTP method a distinct label, highlights endpoint paths, and separates API groups and schemas. It follows the site's light or dark theme and is published as static HTML and CSS without making API requests. References within the specification are resolved; external `$ref` files are not loaded.
