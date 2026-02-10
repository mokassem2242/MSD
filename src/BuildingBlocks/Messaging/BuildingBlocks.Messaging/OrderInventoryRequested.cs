namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when an order has been paid and inventory reservation is requested.
/// Publisher: Order Service (after receiving PaymentSucceeded).
/// Consumer: Inventory Service
/// </summary>
public record OrderInventoryRequested : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public List<RequestedItemDto> Items { get; init; } = new();
    public DateTime RequestedAt { get; init; }

    public OrderInventoryRequested(
        Guid orderId,
        List<RequestedItemDto> items,
        DateTime requestedAt)
    {
        OrderId = orderId;
        Items = items;
        RequestedAt = requestedAt;
    }
}

public record RequestedItemDto
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
}
