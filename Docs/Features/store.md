---
sidebar.visible: false
---

# Online Store

The online store is the customer-facing application for browsing the catalog and placing orders. It owns product descriptions, search, the shopping basket, and the customer experience. Order processing and inventory are handled by separate services.

## Main interactions

- Customers browse products and submit a basket from the web shop.
- The store sends checkout requests to the [Order Service](order-service.md).
- Product availability is shown using data from the [Inventory System](inventory.md).
- Identity, payment, shipping, and email are provided by external systems shown in the [context diagram](Diagrams.md#context-diagrams).

The links in the [flowchart example](Diagrams.md#flowcharts) show how a diagram node can open this detail page.
