namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when a refund is requested for an order (e.g. compensation after inventory failure).
/// Publisher: Order Service
/// Consumer: Payment Service
/// </summary>
public record RefundRequested : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; }
    public DateTime RequestedAt { get; init; }

    public RefundRequested(
        Guid orderId,
        string reason,
        DateTime requestedAt)
    {
        OrderId = orderId;
        Reason = reason ?? "Refund requested";
        RequestedAt = requestedAt;
    }
}
