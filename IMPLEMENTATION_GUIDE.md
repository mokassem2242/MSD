# Implementation Guide - Step by Step

This guide provides a structured approach to implementing the Event-Driven Order Processing System. Follow these steps in order, implementing and testing each part before moving to the next.

---

## Phase 1: Foundation - Building Blocks

### Step 1: EventBus Abstraction ⭐ START HERE

**Files to create:**
- `src/BuildingBlocks/EventBus/BuildingBlocks.EventBus/IEventBus.cs`
- `src/BuildingBlocks/EventBus/BuildingBlocks.EventBus/InMemoryEventBus.cs`

**What to implement:**
1. `IEventBus` interface with methods:
   - `Task PublishAsync<T>(T integrationEvent)` where T : IIntegrationEvent
   - `void Subscribe<T>(Func<T, Task> handler)` where T : IIntegrationEvent

2. `InMemoryEventBus` implementation:
   - Dictionary to store event handlers by event type
   - Publish method that finds handlers and invokes them
   - Subscribe method that registers handlers

**Checkpoint:** ✅ Can publish and subscribe to events in memory

---

### Step 2: Messaging Contracts

**Files to create:**
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/IIntegrationEvent.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/IntegrationEvent.cs` (base class)
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/OrderCreated.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/PaymentSucceeded.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/PaymentFailed.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/InventoryReserved.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/InventoryFailed.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/PaymentRefunded.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/OrderCompleted.cs`
- `src/BuildingBlocks/Messaging/BuildingBlocks.Messaging/OrderCancelled.cs`

**What to implement:**
1. `IIntegrationEvent` interface:
   - `Guid Id { get; }`
   - `DateTime OccurredOn { get; }`

2. `IntegrationEvent` base class implementing `IIntegrationEvent`

3. All event classes inheriting from `IntegrationEvent`:
   - Include only essential fields from the architecture docs
   - Use immutable records if preferred

**Reference:** See "Event Contracts" section in `canonical_distributed_systems_practice_project (3).md`

**Checkpoint:** ✅ All event contracts defined with correct properties

---

### Step 3: SharedKernel (Base Classes)

**Files to create:**
- `src/BuildingBlocks/SharedKernel/BuildingBlocks.SharedKernel/Entity.cs`
- `src/BuildingBlocks/SharedKernel/BuildingBlocks.SharedKernel/AggregateRoot.cs`
- `src/BuildingBlocks/SharedKernel/BuildingBlocks.SharedKernel/ValueObject.cs`
- `src/BuildingBlocks/SharedKernel/BuildingBlocks.SharedKernel/DomainEvent.cs`

**What to implement:**
1. `Entity<TId>` base class:
   - `TId Id { get; protected set; }`
   - Equality comparison based on Id

2. `AggregateRoot<TId>` inheriting from `Entity<TId>`:
   - `List<DomainEvent> DomainEvents { get; }`
   - `void AddDomainEvent(DomainEvent domainEvent)`
   - `void ClearDomainEvents()`

3. `ValueObject` base class:
   - Equality comparison based on all properties

4. `DomainEvent` base class:
   - `DateTime OccurredOn { get; }`

**Checkpoint:** ✅ Base classes ready for use in domain models

---

## Phase 2: Order Module (Core Business Logic)

### Step 4: Order Domain Model

**Files to create:**
- `src/Order/Order.Domain/Order.Domain/Enums/OrderStatus.cs`
- `src/Order/Order.Domain/Order.Domain/ValueObjects/OrderItem.cs`
- `src/Order/Order.Domain/Order.Domain/Aggregates/Order.cs`
- `src/Order/Order.Domain/Order.Domain/Events/OrderCreatedDomainEvent.cs`

**What to implement:**
1. `OrderStatus` enum:
   - Pending, Paid, Completed, Cancelled

2. `OrderItem` value object:
   - ProductId, Quantity, Price
   - Immutable

3. `Order` aggregate root:
   - Properties: Id, CustomerId, OrderItems, Status, TotalAmount, CreatedAt
   - Methods:
     - `Create(customerId, items)` - static factory method
     - `MarkAsPaid()`
     - `MarkAsCompleted()`
     - `Cancel(reason)`
   - Domain events: Raise `OrderCreatedDomainEvent` on creation
   - Business rules: Validate order items, calculate total

**Checkpoint:** ✅ Order aggregate can be created and state transitions work

---

### Step 5: Order Application - Commands & Handlers

**Files to create:**
- `src/Order/Order.Application/Order.Application/Commands/CreateOrderCommand.cs`
- `src/Order/Order.Application/Order.Application/Handlers/CreateOrderCommandHandler.cs`
- `src/Order/Order.Application/Order.Application/Ports/IOrderRepository.cs`

**What to implement:**
1. `CreateOrderCommand`:
   - CustomerId, Items (list of order item DTOs)

2. `IOrderRepository` interface:
   - `Task<Order> GetByIdAsync(Guid orderId)`
   - `Task AddAsync(Order order)`
   - `Task UpdateAsync(Order order)`

3. `CreateOrderCommandHandler`:
   - Validate command
   - Create Order aggregate using factory method
   - Save to repository
   - Publish `OrderCreated` integration event via EventBus
   - Return order ID

**Checkpoint:** ✅ Can create orders through command handler

---

### Step 6: Order Infrastructure - Persistence

**Files to create:**
- `src/Order/Order.Infrastructure/Order.Infrastructure/Persistence/OrderDbContext.cs`
- `src/Order/Order.Infrastructure/Order.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
- `src/Order/Order.Infrastructure/Order.Infrastructure/Repositories/OrderRepository.cs`

**What to implement:**
1. `OrderDbContext`:
   - `DbSet<Order> Orders { get; set; }`
   - Configure in `OnModelCreating` to use configurations

2. `OrderConfiguration`:
   - Map Order aggregate to database
   - Configure owned types (OrderItem as value object)
   - Configure enum conversion

3. `OrderRepository`:
   - Implement `IOrderRepository`
   - Use `OrderDbContext` to persist orders
   - Clear domain events after save

**Dependencies:** Install Entity Framework Core packages

**Checkpoint:** ✅ Orders can be persisted to database

---

### Step 7: Order API - Controller

**Files to create:**
- `src/Order/Order.Api/Order.Api/Controllers/OrdersController.cs`
- `src/Order/Order.Api/Order.Api/DTOs/CreateOrderRequest.cs`
- `src/Order/Order.Api/Order.Api/DTOs/OrderResponse.cs`

**What to implement:**
1. `CreateOrderRequest` DTO:
   - CustomerId, Items

2. `OrderResponse` DTO:
   - OrderId, Status, TotalAmount, CreatedAt

3. `OrdersController`:
   - `POST /api/orders` - Create order
   - Map request to command
   - Call command handler
   - Return response
   - Handle errors appropriately

**Checkpoint:** ✅ Can create orders via HTTP endpoint

---

## Phase 3: Saga Orchestration (Event Handlers)

### Step 8: Order Event Handlers (Saga Logic)

**Files to create:**
- `src/Order/Order.Application/Order.Application/EventHandlers/PaymentSucceededEventHandler.cs`
- `src/Order/Order.Application/Order.Application/EventHandlers/PaymentFailedEventHandler.cs`
- `src/Order/Order.Application/Order.Application/EventHandlers/InventoryReservedEventHandler.cs`
- `src/Order/Order.Application/Order.Application/EventHandlers/InventoryFailedEventHandler.cs`
- `src/Order/Order.Application/Order.Application/EventHandlers/PaymentRefundedEventHandler.cs`

**What to implement:**
1. Each event handler:
   - Load order by orderId from event
   - Check if already processed (idempotency check)
   - Update order state appropriately
   - Publish next event in saga if needed
   - Save order

2. Saga flow:
   - `PaymentSucceeded` → Mark order as Paid → Publish `OrderInventoryRequested` (or just trigger inventory check)
   - `PaymentFailed` → Cancel order
   - `InventoryReserved` → Mark order as Completed → Publish `OrderCompleted`
   - `InventoryFailed` → Request payment refund → Publish refund command
   - `PaymentRefunded` → Cancel order

**Note:** For now, you can publish inventory request as an event. In a real system, this would be a command.

**Checkpoint:** ✅ Saga orchestration works for happy path and failure paths

---

## Phase 4: Payment & Inventory Modules (Participants)

### Step 9: Payment Module - Domain & Application

**Implement similar structure to Order:**
1. Payment Domain: `Payment` aggregate with status (Pending, Succeeded, Failed, Refunded)
2. Payment Application: Command handlers for processing payment and refund
3. Payment Infrastructure: DbContext and repository
4. Payment API: Controller for payment operations
5. Event handlers: Listen to `OrderCreated` (or `OrderPaymentRequested`), process payment, publish events

### Step 10: Inventory Module - Domain & Application

**Implement similar structure:**
1. Inventory Domain: `InventoryItem` aggregate, `Reservation` aggregate
2. Inventory Application: Command handlers for reservation
3. Inventory Infrastructure: DbContext and repository
4. Inventory API: Controller for inventory operations
5. Event handlers: Listen to `OrderInventoryRequested`, reserve inventory, publish events

---

## Phase 5: Wiring Everything Together

### Step 11: Configure Dependency Injection in WebHost

**Update:**
- `host/Bootstrapper/WebHost/WebHost/Program.cs`

**What to configure:**
1. Register EventBus (singleton)
2. Register all DbContexts with connection strings
3. Register repositories
4. Register command handlers
5. Register event handlers and subscribe them to EventBus
6. Register services from all modules

**Checkpoint:** ✅ All services registered and EventBus has all subscriptions

---

## Testing Checklist

After implementing each phase, test:

- [ ] Can create an order via API
- [ ] Order is persisted to database
- [ ] OrderCreated event is published
- [ ] Payment service receives OrderCreated and processes payment
- [ ] PaymentSucceeded event triggers inventory request
- [ ] Inventory service reserves items
- [ ] Order completes successfully
- [ ] Payment failure cancels order correctly
- [ ] Inventory failure triggers refund and order cancellation

---

## Learning Tips

1. **Implement one step at a time** - Don't jump ahead
2. **Test as you go** - Create simple console apps or unit tests to verify each component
3. **Reference the architecture docs** - They have all the event contracts and flows defined
4. **Focus on boundaries** - Remember: modules should NOT reference each other's Domain layers
5. **Keep it simple first** - You can add complexity (Outbox, idempotency keys) later

---

## When You're Ready for Review

Come back when you've completed:
- [ ] Phase 1 (Building Blocks)
- [ ] Phase 2 (Order Module)
- [ ] Phase 3 (Saga Orchestration)

Or if you get stuck on a specific part, ask for help on that particular step!

---

## Key Principles to Remember

1. **Domain events** (within aggregate) vs **Integration events** (between modules)
2. **Commands** express intent (what you want to happen)
3. **Events** express facts (what has happened)
4. **No cross-module domain references** - only event contracts
5. **Order Service orchestrates** - it knows the full saga flow
6. **Participant services are reactive** - they react to events

Good luck with your implementation! 🚀

