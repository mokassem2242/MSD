namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when an order has been successfully completed (paid and inventory reserved).
/// Publisher: Order Service
/// </summary>
public record OrderCompleted : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public DateTime CompletedAt { get; init; }

    public OrderCompleted(
        Guid orderId,
        DateTime completedAt)
    {
        OrderId = orderId;
        CompletedAt = completedAt;
    }
}

