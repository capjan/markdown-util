---
navigation.visible: false
openapi: ./openapi-petstore.json
---

# Swagger Petstore example

This page renders the [Swagger Petstore](https://petstore.swagger.io/) sample API, a larger reference than the [short OpenAPI Spec example](openapi-example.md). The specification uses the Swagger 2.0 format and contains pets, store orders, and users with path, query, header, and form parameters, enums, and file uploads.

The page needs only this front matter:

```yaml
---
openapi: ./openapi-petstore.json
---
```

`openapi-petstore.json` is the specification from `https://petstore.swagger.io/v2/swagger.json`, formatted with indentation. The specification declares the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0.html).
