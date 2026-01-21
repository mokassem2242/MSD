namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when payment for an order has been successfully processed.
/// Publisher: Payment Service
/// </summary>
public record PaymentSucceeded : IntegrationEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public DateTime ProcessedAt { get; init; }

    public PaymentSucceeded(
        Guid paymentId,
        Guid orderId,
        decimal amount,
        DateTime processedAt)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        ProcessedAt = processedAt;
    }
}

