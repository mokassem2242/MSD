# Unit Test Tasks — Saga Event Handlers

Add unit tests for the Order saga integration event handlers and the Payment refund handler. Use the existing test style in this repo: **xunit**, **Moq**, and the patterns from `Order.UnitTests/Application/` and `Inventory.UnitTests/Application/OrderInventoryRequestedEventHandlerTests.cs`.

---

## 1. Order Service — Saga Event Handlers

**Project:** `src/Order/Order.UnitTests/`  
**Reference:** `Order.Application` is already referenced in `Order.UnitTests.csproj`.

Create test classes under `Order.UnitTests/Application/EventHandlers/` (create the `EventHandlers` folder if needed).

---

### 1.1 PaymentSucceededEventHandlerTests.cs

**Handler:** `Order.Application/EventHandlers/PaymentSucceededEventHandler.cs`  
**Dependencies to mock:** `IOrderRepository`, `IEventBus`, `ILogger<PaymentSucceededEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | Test that constructor throws `ArgumentNullException` when `orderRepository`, `eventBus`, or `logger` is null. |
| 2 | Order not found | `GetByIdAsync` returns null → handler returns without calling `UpdateAsync` or `PublishAsync`. |
| 3 | Idempotency — already Paid | Order exists with `Status = Paid` → handler returns without calling `MarkAsPaid` / `UpdateAsync` / `PublishAsync`. |
| 4 | Idempotency — already Completed/Cancelled | Same as above for Completed and Cancelled. |
| 5 | Happy path | Order exists with `Status = Pending` → `UpdateAsync` called once, `PublishAsync` called once with an `OrderInventoryRequested` whose `OrderId` and items match the order. |
| 6 | OrderInventoryRequested shape | Verify published event has correct `OrderId` and `Items` (e.g. from `OrderItems` with `ProductId`, `Quantity`). |

**Tip:** Build a valid `Order` aggregate (e.g. via `Order.Create("customer-1", items)`) and mock `GetByIdAsync` to return it.

---

### 1.2 PaymentFailedEventHandlerTests.cs

**Handler:** `Order.Application/EventHandlers/PaymentFailedEventHandler.cs`  
**Dependencies:** `IOrderRepository`, `ILogger<PaymentFailedEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | `orderRepository` null and `logger` null each throw `ArgumentNullException`. |
| 2 | Order not found | `GetByIdAsync` returns null → no `UpdateAsync`. |
| 3 | Idempotent — already Cancelled | Order status `Cancelled` → handler returns without calling `Cancel` / `UpdateAsync`. |
| 4 | Cannot cancel Completed | Order status `Completed` → handler returns without changing (or document that it does not cancel). |
| 5 | Happy path | Order exists (e.g. Pending or Paid) → `Cancel(failureReason)` and `UpdateAsync` called once. |

Use `PaymentFailed` event with `OrderId`, `FailureReason`, etc., from `BuildingBlocks.Messaging`.

---

### 1.3 InventoryReservedEventHandlerTests.cs

**Handler:** `Order.Application/EventHandlers/InventoryReservedEventHandler.cs`  
**Dependencies:** `IOrderRepository`, `ILogger<InventoryReservedEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | `orderRepository` null and `logger` null throw `ArgumentNullException`. |
| 2 | Order not found | `GetByIdAsync` returns null → no `UpdateAsync`. |
| 3 | Idempotent — already Completed | Order status `Completed` → no `MarkAsCompleted` / `UpdateAsync`. |
| 4 | Wrong state (e.g. Pending) | Order status `Pending` → handler does not call `MarkAsCompleted` (or document expected behavior). |
| 5 | Happy path | Order status `Paid` → `MarkAsCompleted()` and `UpdateAsync` called once. |

Use `InventoryReserved` from `BuildingBlocks.Messaging` with `OrderId`, `ReservationId`, `Items`, `ReservedAt`.

---

### 1.4 InventoryFailedEventHandlerTests.cs

**Handler:** `Order.Application/EventHandlers/InventoryFailedEventHandler.cs`  
**Dependencies:** `IOrderRepository`, `IEventBus`, `ILogger<InventoryFailedEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | `orderRepository`, `eventBus`, `logger` each null → `ArgumentNullException`. |
| 2 | Order not found | `GetByIdAsync` returns null → still publish `RefundRequested` (handler does not early-exit on null order). If your implementation skips publish when order is null, test that instead.) |
| 3 | Happy path | Order exists → `PublishAsync` called once with `RefundRequested` containing correct `OrderId` and `Reason` (e.g. contains `FailureReason` from `InventoryFailed`). |

**Note:** Align tests with actual implementation: if the handler publishes RefundRequested even when order is null, test that; if it skips when order is null, test that.

---

### 1.5 PaymentRefundedEventHandlerTests.cs

**Handler:** `Order.Application/EventHandlers/PaymentRefundedEventHandler.cs`  
**Dependencies:** `IOrderRepository`, `ILogger<PaymentRefundedEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | `orderRepository` null and `logger` null throw `ArgumentNullException`. |
| 2 | Order not found | `GetByIdAsync` returns null → no `UpdateAsync`. |
| 3 | Idempotent — already Cancelled | Order status `Cancelled` → no `Cancel` / `UpdateAsync`. |
| 4 | Happy path | Order exists (e.g. Paid) → `Cancel(reason)` and `UpdateAsync` called once; reason can include refund id or message. |

Use `PaymentRefunded` with `OrderId`, `RefundId`, etc.

---

## 2. Payment Service — RefundRequestedEventHandler

**Current state:** There is no `Payment.UnitTests` project in the repo.

Choose one:

- **Option A — New project:** Create `src/Payment/Payment.UnitTests/` (e.g. `Payment.UnitTests.csproj`) with references to `Payment.Application`, `BuildingBlocks.Messaging`, and test packages (xunit, Moq, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio). Then add the tests below.
- **Option B — Reuse host tests:** If you have (or add) a host/integration test project that references Payment.Application, add these tests there instead.

---

### 2.1 RefundRequestedEventHandlerTests.cs

**Handler:** `Payment.Application/Handlers/RefundRequestedEventHandler.cs`  
**Dependencies:** `IPaymentRepository`, `RefundPaymentCommandHandler`, `ILogger<RefundRequestedEventHandler>`

| # | Task | Description |
|---|------|-------------|
| 1 | Constructor null checks | Each of `paymentRepository`, `refundHandler`, `logger` null → `ArgumentNullException`. |
| 2 | No payment for order | `GetByOrderIdAsync` returns null → handler returns without calling `RefundPaymentCommandHandler.HandleAsync`. |
| 3 | Happy path | `GetByOrderIdAsync` returns a payment → `RefundPaymentCommandHandler.HandleAsync` called once with `RefundPaymentCommand(payment.Id)`. |

**Tip:** Mock `IPaymentRepository.GetByOrderIdAsync` and a real or mock `RefundPaymentCommandHandler`; if you mock the handler, verify `HandleAsync(RefundPaymentCommand)` with the correct `PaymentId`.

---

## 3. Running and Completing

- Run Order tests:  
  `dotnet test src/Order/Order.UnitTests/Order.UnitTests.csproj`
- If you add Payment.UnitTests:  
  `dotnet test src/Payment/Payment.UnitTests/Payment.UnitTests.csproj`
- Run all tests:  
  `dotnet test`

Mark each task in the tables above as done as you implement the tests. Adjust any “document expected behavior” or “align with implementation” items after you read the handler code.
