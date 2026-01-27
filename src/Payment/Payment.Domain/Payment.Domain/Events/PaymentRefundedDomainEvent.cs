using BuildingBlocks.SharedKernel;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment has been refunded.
/// This event is raised within the Payment aggregate and will be converted
/// to a PaymentRefunded integration event in the Infrastructure layer.
/// </summary>
public class PaymentRefundedDomainEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public DateTime RefundedAt { get; }

    public PaymentRefundedDomainEvent(
        Guid paymentId,
        Guid orderId,
        decimal amount,
        DateTime refundedAt)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        RefundedAt = refundedAt;
    }
}
