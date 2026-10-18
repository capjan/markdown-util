---
sidebar.visible: false
---

# Inventory System

The Inventory System tracks sellable quantities across the shop's warehouses. It is the source of truth for available stock and prevents two checkouts from claiming the same units.

## Reservations and updates

- The store reads availability while customers browse the catalog.
- The [Order Service](order-service.md) creates a short-lived reservation during checkout.
- A successful order converts the reservation into a committed allocation; a cancelled or expired order releases it.
- Warehouse adjustments and completed shipments update the on-hand quantity and publish stock-change events.

The [context diagram example](Diagrams.md#context-diagrams) links the Inventory node to this detail page.
