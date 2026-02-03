using BuildingBlocks.SharedKernel;

namespace Order.Domain.Aggregates.Order.Domain.Events;

/// <summary>
/// Domain event raised when an order is marked as completed.
/// </summary>
public class OrderCompletedDomainEvent : DomainEvent
{
    public Guid OrderId { get; }
    public DateTime CompletedAt { get; }

    public OrderCompletedDomainEvent(Guid orderId, DateTime completedAt)
    {
        OrderId = orderId;
        CompletedAt = completedAt;
    }
}

