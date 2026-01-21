using BuildingBlocks.SharedKernel;

namespace Order.Domain.Events;

/// <summary>
/// Domain event raised when an order is created.
/// This event is raised within the Order aggregate and can be published
/// as an integration event to other modules.
/// </summary>
public class OrderCreatedDomainEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string CustomerId { get; }
    public decimal TotalAmount { get; }
    public DateTime CreatedAt { get; }

    public OrderCreatedDomainEvent(
        Guid orderId,
        string customerId,
        decimal totalAmount,
        DateTime createdAt)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        CreatedAt = createdAt;
    }
}

