using BuildingBlocks.SharedKernel;

namespace Payment.Domain.Events;

/// <summary>
/// Domain event raised when a payment has been successfully processed.
/// This event is raised within the Payment aggregate and will be converted
/// to a PaymentSucceeded integration event in the Infrastructure layer.
/// </summary>
public class PaymentSucceededDomainEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public DateTime ProcessedAt { get; }

    public PaymentSucceededDomainEvent(
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
