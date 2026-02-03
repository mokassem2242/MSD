using BuildingBlocks.SharedKernel;

namespace Order.Domain.Aggregates.Order.Domain.Events;

/// <summary>
/// Domain event raised when an order is cancelled.
/// </summary>
public class OrderCancelledDomainEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string? Reason { get; }
    public DateTime CancelledAt { get; }

    public OrderCancelledDomainEvent(Guid orderId, string? reason, DateTime cancelledAt)
    {
        OrderId = orderId;
        Reason = reason;
        CancelledAt = cancelledAt;
    }
}

