namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when an order has been cancelled (payment failed or compensation completed).
/// Publisher: Order Service
/// </summary>
public record OrderCancelled : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CancellationReason { get; init; }
    public DateTime CancelledAt { get; init; }

    public OrderCancelled(
        Guid orderId,
        string cancellationReason,
        DateTime cancelledAt)
    {
        OrderId = orderId;
        CancellationReason = cancellationReason;
        CancelledAt = cancelledAt;
    }
}

