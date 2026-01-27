using BuildingBlocks.SharedKernel;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when payment processing has failed.
/// This event is raised within the Payment aggregate and will be converted
/// to a PaymentFailed integration event in the Infrastructure layer.
/// </summary>
public class PaymentFailedDomainEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public string FailureReason { get; }
    public DateTime FailedAt { get; }

    public PaymentFailedDomainEvent(
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
