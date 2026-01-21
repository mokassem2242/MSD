namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when a new order has been created and is ready for processing.
/// Publisher: Order Service
/// </summary>
public record OrderCreated : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }

    public OrderCreated(
        Guid orderId,
        string customerId,
        decimal totalAmount,
        List<OrderItemDto> items,
        DateTime createdAt)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Items = items;
        CreatedAt = createdAt;
    }
}

public record OrderItemDto
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

