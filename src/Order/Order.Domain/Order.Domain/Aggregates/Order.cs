using BuildingBlocks.SharedKernel;
using Order.Domain.Aggregates.Order.Domain.Enums;
using Order.Domain.Aggregates.Order.Domain.Events;
using Order.Domain.Aggregates.Order.Domain.ValueObjects;

namespace Order.Domain.Aggregates.Order.Domain.Aggregates;

/// <summary>
/// Order aggregate root representing a customer order.
/// Owns the order lifecycle and orchestrates the order saga.
/// </summary>
public class Order : AggregateRoot<Guid>
{
    public string CustomerId { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Order() : base()
    {
        OrderItems = new List<OrderItem>();
    }

    private Order(
        Guid id,
        string customerId,
        List<OrderItem> orderItems,
        DateTime createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId cannot be null or empty", nameof(customerId));

        if (orderItems == null || orderItems.Count == 0)
            throw new ArgumentException("Order must have at least one item", nameof(orderItems));

        CustomerId = customerId;
        OrderItems = orderItems.ToList(); // Create a copy to ensure immutability
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        TotalAmount = CalculateTotalAmount(orderItems);

        // Raise domain event
        AddDomainEvent(new OrderCreatedDomainEvent(
            Id,
            CustomerId,
            TotalAmount,
            CreatedAt));
    }

    /// <summary>
    /// Factory method to create a new order.
    /// </summary>
    public static Order Create(string customerId, List<OrderItem> orderItems)
    {
        return new Order(
            Guid.NewGuid(),
            customerId,
            orderItems,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Marks the order as paid after successful payment processing.
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot mark order as paid. Current status: {Status}");

        Status = OrderStatus.Paid;

        // Raise domain event
        AddDomainEvent(new OrderPaidDomainEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Marks the order as completed after inventory has been reserved.
    /// </summary>
    public void MarkAsCompleted()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException($"Cannot mark order as completed. Current status: {Status}");

        Status = OrderStatus.Completed;

        // Raise domain event
        AddDomainEvent(new OrderCompletedDomainEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Cancels the order with an optional reason.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order");

        if (Status == OrderStatus.Cancelled)
            return; // Already cancelled, idempotent operation

        Status = OrderStatus.Cancelled;

        // Raise domain event
        AddDomainEvent(new OrderCancelledDomainEvent(Id, reason, DateTime.UtcNow));
    }

    /// <summary>
    /// Calculates the total amount for all order items.
    /// </summary>
    private static decimal CalculateTotalAmount(List<OrderItem> orderItems)
    {
        return orderItems.Sum(item => item.GetTotalPrice());
    }
}

