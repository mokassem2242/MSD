namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when inventory has been successfully reserved for an order.
/// Publisher: Inventory Service
/// </summary>
public record InventoryReserved : IntegrationEvent
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
    public List<ReservedItemDto> Items { get; init; } = new();
    public DateTime ReservedAt { get; init; }

    public InventoryReserved(
        Guid reservationId,
        Guid orderId,
        List<ReservedItemDto> items,
        DateTime reservedAt)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        Items = items;
        ReservedAt = reservedAt;
    }
}

public record ReservedItemDto
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
}

