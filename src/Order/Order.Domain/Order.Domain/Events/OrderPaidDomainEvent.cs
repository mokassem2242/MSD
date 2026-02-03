using BuildingBlocks.SharedKernel;

namespace Order.Domain.Aggregates.Order.Domain.Events;

/// <summary>
/// Domain event raised when an order is marked as paid.
/// </summary>
public class OrderPaidDomainEvent : DomainEvent
{
    public Guid OrderId { get; }
    public DateTime PaidAt { get; }

    public OrderPaidDomainEvent(Guid orderId, DateTime paidAt)
    {
        OrderId = orderId;
        PaidAt = paidAt;
    }
}

