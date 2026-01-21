namespace BuildingBlocks.Messaging;

/// <summary>
/// Published when payment processing has failed.
/// Publisher: Payment Service
/// </summary>
public record PaymentFailed : IntegrationEvent
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string FailureReason { get; init; }
    public DateTime FailedAt { get; init; }

    public PaymentFailed(
        Guid paymentId,
        Guid orderId,
        decimal amount,
        string failureReason,
        DateTime failedAt)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        FailureReason = failureReason;
        FailedAt = failedAt;
    }
}

