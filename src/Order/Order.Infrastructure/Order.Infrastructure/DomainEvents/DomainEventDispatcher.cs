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
        foreach (var domainEvent in domainEvents)
        {
            IIntegrationEvent? integrationEvent = domainEvent switch
            {
                OrderCreatedDomainEvent created when aggregate is OrderAggregate order 
                    => ConvertToOrderCreated(created, order),
                OrderPaidDomainEvent paid => ConvertToOrderPaid(paid),
                OrderCompletedDomainEvent completed => ConvertToOrderCompleted(completed),
                OrderCancelledDomainEvent cancelled => ConvertToOrderCancelled(cancelled),
                _ => null
            };

            if (integrationEvent != null)
            {
                await _eventBus.PublishAsync(integrationEvent);
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

