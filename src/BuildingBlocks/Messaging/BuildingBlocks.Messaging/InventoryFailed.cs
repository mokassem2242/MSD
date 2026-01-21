namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when inventory reservation has failed (e.g., out of stock).
/// Publisher: Inventory Service
/// </summary>
public record InventoryFailed : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string FailureReason { get; init; }
    public List<FailedItemDto> FailedItems { get; init; } = new();
    public DateTime FailedAt { get; init; }

    public InventoryFailed(
        Guid orderId,
        string failureReason,
        List<FailedItemDto> failedItems,
        DateTime failedAt)
    {
        OrderId = orderId;
        FailureReason = failureReason;
        FailedItems = failedItems;
        FailedAt = failedAt;
    }
}

public record FailedItemDto
{
    public required string ProductId { get; init; }
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
}

