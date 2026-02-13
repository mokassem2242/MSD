# Next Steps — Event-Driven Order System (Practice Demo)

**Last updated:** Session note for upcoming work.

---

## Immediate Next Step: Order Saga Integration Event Handlers

The Order service is the **saga orchestrator** but currently **does not subscribe** to integration events from Payment or Inventory. The saga (Create → Pay → Reserve → Complete / Cancel / Refund) does not run end-to-end.

### What to Implement

1. **Order.Application event handlers** (new files under `src/Order/Order.Application/Order.Application/`):
   - **`EventHandlers/PaymentSucceededEventHandler.cs`** — Load order by OrderId, idempotency check, `MarkAsPaid()`, save, **publish `OrderInventoryRequested`** (order id + items).
   - **`EventHandlers/PaymentFailedEventHandler.cs`** — Load order, idempotency check, `Cancel(reason)`, save.
   - **`EventHandlers/InventoryReservedEventHandler.cs`** — Load order, idempotency check, `MarkAsCompleted()`, save (domain event will publish OrderCompleted via existing DomainEventDispatcher).
   - **`EventHandlers/InventoryFailedEventHandler.cs`** — Load order, idempotency check, trigger compensation: request refund (publish/command to Payment), then on PaymentRefunded cancel order.
   - **`EventHandlers/PaymentRefundedEventHandler.cs`** — Load order, idempotency check, `Cancel(reason)`, save.

2. **Wire handlers in host** — In `host/Bootstrapper/WebHost/WebHost/Program.cs`, after existing `Subscribe<OrderCreated>` and `Subscribe<OrderInventoryRequested>`, add subscriptions that resolve from scope and call:
   - `PaymentSucceededEventHandler` for `PaymentSucceeded`
   - `PaymentFailedEventHandler` for `PaymentFailed`
   - `InventoryReservedEventHandler` for `InventoryReserved`
   - `InventoryFailedEventHandler` for `InventoryFailed`
   - `PaymentRefundedEventHandler` for `PaymentRefunded`

3. **Messaging** — Ensure `OrderInventoryRequested` (and any refund-request event/command) exists in `BuildingBlocks.Messaging` and that Order.Application references BuildingBlocks.Messaging and BuildingBlocks.EventBus.

### Reference in Repo

- **IMPLEMENTATION_GUIDE.md** — Phase 3, Step 8: "Order Event Handlers (Saga Logic)".
- **README.md** — Saga flow diagrams (happy path, payment failure, inventory failure + compensation).

---

## After the Saga Works

| Priority | Task | Notes |
|----------|------|--------|
| 1 | **Notification module** | Handlers for OrderCompleted, OrderCancelled, PaymentFailed (e.g. log or in-memory). |
| 2 | **Analytics module** | Handlers for OrderCreated, OrderCompleted, OrderCancelled, PaymentSucceeded, PaymentFailed (e.g. append to list or simple store). |
| 3 | **Idempotency** | Event/correlation id in Order (and Payment/Inventory) so duplicate events don’t double-apply. |
| 4 | **Outbox** | Implement in BuildingBlocks and use in Order (and optionally Payment) for reliable publish-after-commit. |
| 5 | **E2E test** | Create order and assert final order status (and optionally that Payment/Inventory were invoked). |

---

## Later (Optional)

- Replace in-memory EventBus with RabbitMQ.
- Add Redis for read-side caching (e.g. order queries, product availability).
