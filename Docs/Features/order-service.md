---
navigation.visible: false
---

# Order Service

The Order Service turns a customer's checkout request into a durable order. It validates prices and customer details, reserves stock, starts payment, and publishes order events for fulfillment.

## Order flow

1. The service checks the basket and creates an order with a stable request identifier so a retry cannot create a duplicate.
2. It asks the [Inventory System](inventory.md) to reserve the requested quantities.
3. It requests payment authorization and stores the resulting status with the order.
4. It publishes an order-confirmed event after payment succeeds; fulfillment and notification services consume that event.

If a reservation or payment fails, the order remains visible with a failure status and any successful reservation is released. The [component diagram example](Diagrams.md#plantuml-component-diagrams) links its Order Service node to this page.
