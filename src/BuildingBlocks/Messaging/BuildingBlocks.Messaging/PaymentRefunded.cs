namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when a payment has been refunded (compensation for failed inventory).
/// Publisher: Payment Service
/// </summary>
public record PaymentRefunded : IntegrationEvent
{
    public Guid RefundId { get; init; }
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public DateTime RefundedAt { get; init; }

    public PaymentRefunded(
        Guid refundId,
        Guid paymentId,
        Guid orderId,
        decimal amount,
        DateTime refundedAt)
    {
        RefundId = refundId;
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        RefundedAt = refundedAt;
    }
}

