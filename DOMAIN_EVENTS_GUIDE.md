# Domain Events - Where They Should Be Emitted

## Overview

Domain events represent **something meaningful that happened** in the domain. They should be raised **within the aggregate** when state changes occur, then collected and published as integration events.

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Aggregate (Domain Layer)                                  │
│    - State changes occur (MarkAsPaid, MarkAsCompleted, etc.) │
│    - Domain events are raised: AddDomainEvent(...)          │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Application Layer (Command Handler)                      │
│    - Collects domain events BEFORE saving                   │
│    - Saves aggregate to repository                          │
│    - Converts domain events → integration events             │
│    - Publishes integration events via EventBus               │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Infrastructure Layer (Repository)                        │
│    - Saves aggregate to database                            │
│    - Clears domain events (safety measure)                  │
└─────────────────────────────────────────────────────────────┘
```

## Where Domain Events Are Emitted

### ✅ **IN THE AGGREGATE** (Domain Layer)

Domain events should be raised **inside the aggregate** when business state changes:

```csharp
// ✅ CORRECT: Domain event raised in aggregate
public void MarkAsPaid()
{
    if (Status != OrderStatus.Pending)
        throw new InvalidOperationException(...);

    Status = OrderStatus.Paid;
    
    // Raise domain event when state changes
    AddDomainEvent(new OrderPaidDomainEvent(Id, DateTime.UtcNow));
}
```

**Why in the aggregate?**
- Domain events are part of the domain model
- They represent business facts that occurred
- They ensure events are raised whenever state changes
- They keep business logic encapsulated

### ❌ **NOT in Application Layer**

```csharp
// ❌ WRONG: Don't raise domain events in handlers
public async Task HandleAsync(CreateOrderCommand command)
{
    var order = Order.Create(...);
    await _repository.AddAsync(order);
    
    // ❌ Don't do this - domain events should be in aggregate
    order.AddDomainEvent(new OrderCreatedDomainEvent(...));
}
```

### ❌ **NOT in Infrastructure Layer**

```csharp
// ❌ WRONG: Don't raise domain events in repository
public async Task AddAsync(Order order)
{
    await _context.Orders.AddAsync(order);
    await _context.SaveChangesAsync();
    
    // ❌ Don't do this - domain events should be in aggregate
    order.AddDomainEvent(new OrderCreatedDomainEvent(...));
}
```

## Complete Example

### 1. Domain Event Raised in Aggregate

```csharp
// Order.cs (Domain Layer)
public void MarkAsCompleted()
{
    if (Status != OrderStatus.Paid)
        throw new InvalidOperationException(...);

    Status = OrderStatus.Completed;
    
    // ✅ Domain event raised here
    AddDomainEvent(new OrderCompletedDomainEvent(Id, DateTime.UtcNow));
}
```

### 2. Domain Events Collected in Application Layer

```csharp
// OrderCompletedEventHandler.cs (Application Layer)
public async Task HandleAsync(InventoryReservedEvent @event)
{
    // Load aggregate
    var order = await _repository.GetByIdAsync(@event.OrderId);
    
    // Change state (this raises domain event)
    order.MarkAsCompleted();
    
    // ✅ Collect domain events BEFORE saving
    var domainEvents = order.DomainEvents.ToList();
    
    // Save aggregate (repository will clear events)
    await _repository.UpdateAsync(order);
    
    // ✅ Convert domain events to integration events
    foreach (var domainEvent in domainEvents)
    {
        if (domainEvent is OrderCompletedDomainEvent completed)
        {
            var integrationEvent = new OrderCompleted(
                completed.OrderId,
                completed.CompletedAt
            );
            
            // ✅ Publish integration event
            await _eventBus.PublishAsync(integrationEvent);
        }
    }
}
```

### 3. Repository Clears Events (Safety)

```csharp
// OrderRepository.cs (Infrastructure Layer)
public async Task UpdateAsync(Order aggregate)
{
    _context.Orders.Update(aggregate);
    await _context.SaveChangesAsync();
    
    // ✅ Clear events after save (safety measure)
    // Note: Application layer should have already collected them
    aggregate.ClearDomainEvents();
}
```

## All Domain Events in Order Aggregate

Currently implemented domain events:

1. **OrderCreatedDomainEvent** - Raised in `Order.Create()` constructor
2. **OrderPaidDomainEvent** - Raised in `MarkAsPaid()`
3. **OrderCompletedDomainEvent** - Raised in `MarkAsCompleted()`
4. **OrderCancelledDomainEvent** - Raised in `Cancel()`

## Key Principles

1. **Domain events = Domain layer** - They represent business facts
2. **Raise events when state changes** - Not before, not after
3. **Collect before saving** - Application layer collects before repository clears
4. **Convert to integration events** - Application layer converts domain → integration
5. **Publish via EventBus** - Integration events go to other modules

## Benefits

✅ **Encapsulation** - Business logic and events stay together  
✅ **Consistency** - Events always raised when state changes  
✅ **Testability** - Can test domain events in unit tests  
✅ **Separation** - Domain events (internal) vs Integration events (external)  
✅ **Reliability** - Events are part of the transaction boundary

