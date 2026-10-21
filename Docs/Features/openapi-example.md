---
navigation.visible: false
openapi: ./openapi-example.yaml
---

# OpenAPI Spec example

This page shows the Markdown front matter and a short OpenAPI document. MDU generates the reference below from the same YAML file.

The Markdown page uses this front matter:

```yaml
---
title: Orders API
openapi: ./openapi-example.yaml
---
```

The referenced `openapi-example.yaml` contains one endpoint and its response schema:

```yaml
openapi: 3.1.0
info:
  title: Orders API
  version: 1.0.0
paths:
  /orders/{id}:
    get:
      tags: [Orders]
      summary: Get an order
      parameters:
        - name: id
          in: path
          required: true
          schema: { type: string }
      responses:
        '200':
          description: Found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Order'
components:
  schemas:
    Order:
      type: object
      required: [id, status]
      properties:
        id: { type: string }
        status:
          type: string
          enum: [pending, shipped]
```

## Generated reference
