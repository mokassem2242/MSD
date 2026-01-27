using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using BuildingBlocks.SharedKernel;
using Order.Domain.Aggregates;
using Order.Domain.Events;
using OrderAggregate = Order.Domain.Aggregates.Order;

namespace Order.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches domain events by converting them to integration events and publishing via EventBus.
/// This service handles the conversion from domain events (internal) to integration events (cross-module).
/// Generic implementation that works with any aggregate type.
/// </summary>
public class DomainEventDispatcher
{
    private readonly IEventBus _eventBus;

    public DomainEventDispatcher(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Dispatches domain events by converting them to integration events and publishing.
    /// Generic method that works with any aggregate root.
    /// </summary>
    public async Task DispatchDomainEventsAsync(
        IEnumerable<DomainEvent> domainEvents, 
        AggregateRoot<Guid> aggregate)
    {
        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "DomainEventDispatcher.DispatchDomainEventsAsync:ENTRY", message = "Dispatching domain events", data = new { aggregateId = aggregate.Id, domainEventCount = domainEvents.Count(), domainEventTypes = domainEvents.Select(e => e.GetType().Name).ToArray() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] DomainEventDispatcher:ENTRY - {domainEvents.Count()} events for aggregate {aggregate.Id}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        foreach (var domainEvent in domainEvents)
        {
            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "DomainEventDispatcher.DispatchDomainEventsAsync:PROCESSING", message = "Processing domain event", data = new { domainEventType = domainEvent.GetType().Name, aggregateType = aggregate.GetType().Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] DomainEventDispatcher:PROCESSING - {domainEvent.GetType().Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            IIntegrationEvent? integrationEvent = domainEvent switch
            {
                OrderCreatedDomainEvent created when aggregate is OrderAggregate order 
                    => ConvertToOrderCreated(created, order),
                OrderPaidDomainEvent paid => ConvertToOrderPaid(paid),
                OrderCompletedDomainEvent completed => ConvertToOrderCompleted(completed),
                OrderCancelledDomainEvent cancelled => ConvertToOrderCancelled(cancelled),
                _ => null
            };

            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "DomainEventDispatcher.DispatchDomainEventsAsync:CONVERSION", message = "Domain to integration event conversion", data = new { domainEventType = domainEvent.GetType().Name, integrationEventType = integrationEvent?.GetType().Name ?? "null", converted = integrationEvent != null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] DomainEventDispatcher:CONVERSION - {domainEvent.GetType().Name} -> {(integrationEvent != null ? integrationEvent.GetType().Name : "NULL")}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            if (integrationEvent != null)
            {
                // #region agent log
                try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "DomainEventDispatcher.DispatchDomainEventsAsync:BEFORE_PUBLISH", message = "About to publish integration event", data = new { integrationEventType = integrationEvent.GetType().Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] DomainEventDispatcher:BEFORE_PUBLISH - {integrationEvent.GetType().Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
                // #endregion

                await _eventBus.PublishAsync(integrationEvent);

                // #region agent log
                try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "DomainEventDispatcher.DispatchDomainEventsAsync:AFTER_PUBLISH", message = "Integration event published", data = new { integrationEventType = integrationEvent.GetType().Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] DomainEventDispatcher:AFTER_PUBLISH - {integrationEvent.GetType().Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
                // #endregion
            }
        }
    }

    private OrderCreated ConvertToOrderCreated(OrderCreatedDomainEvent domainEvent, OrderAggregate order)
    {
        return new OrderCreated(
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.TotalAmount,
            order.OrderItems.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList(),
            domainEvent.CreatedAt
        );
    }

    private IIntegrationEvent? ConvertToOrderPaid(OrderPaidDomainEvent domainEvent)
    {
        // OrderPaid is an internal state change - we don't publish it as integration event
        // PaymentSucceeded comes from Payment service, not Order service
        return null;
    }

    private OrderCompleted ConvertToOrderCompleted(OrderCompletedDomainEvent domainEvent)
    {
        return new OrderCompleted(
            domainEvent.OrderId,
            domainEvent.CompletedAt
        );
    }

    private OrderCancelled ConvertToOrderCancelled(OrderCancelledDomainEvent domainEvent)
    {
        return new OrderCancelled(
            domainEvent.OrderId,
            domainEvent.Reason ?? "Order cancelled",
            domainEvent.CancelledAt
        );
    }
}

