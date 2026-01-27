using BuildingBlocks.SharedKernel;
using Payment.Domain.Enums;
using Payment.Domain.Events;

namespace Payment.Domain.Aggregates;

/// <summary>
/// Payment aggregate root representing a payment transaction.
/// Manages payment lifecycle and raises domain events for state changes.
/// </summary>
public class Payment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public string CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    // EF Core requires a parameterless constructor
    private Payment() : base()
    {
    }

    private Payment(
        Guid id,
        Guid orderId,
        string customerId,
        decimal amount,
        DateTime createdAt) : base(id)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty", nameof(orderId));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId cannot be null or empty", nameof(customerId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
        ProcessedAt = null;
        FailureReason = null;
    }

    /// <summary>
    /// Factory method to create a new payment.
    /// </summary>
    public static Payment Create(Guid orderId, string customerId, decimal amount)
    {
        return new Payment(
            Guid.NewGuid(),
            orderId,
            customerId,
            amount,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Marks the payment as succeeded.
    /// </summary>
    public void MarkAsSucceeded()
    {
        if (Status == PaymentStatus.Succeeded)
            return; // Already succeeded, idempotent operation

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark payment as succeeded. Current status: {Status}");

        Status = PaymentStatus.Succeeded;
        ProcessedAt = DateTime.UtcNow;

        // Raise domain event
        AddDomainEvent(new PaymentSucceededDomainEvent(
            Id,
            OrderId,
            Amount,
            ProcessedAt.Value));
    }

    /// <summary>
    /// Marks the payment as failed with an optional reason.
    /// </summary>
    public void MarkAsFailed(string? reason = null)
    {
        if (Status == PaymentStatus.Failed)
            return; // Already failed, idempotent operation

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark payment as failed. Current status: {Status}");

        Status = PaymentStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
        FailureReason = reason ?? "Payment processing failed";

        // Raise domain event
        AddDomainEvent(new PaymentFailedDomainEvent(
            Id,
            OrderId,
            Amount,
            FailureReason,
            ProcessedAt.Value));
    }

    /// <summary>
    /// Refunds the payment (compensation for failed inventory or cancellation).
    /// </summary>
    public void Refund()
    {
        if (Status == PaymentStatus.Refunded)
            return; // Already refunded, idempotent operation

        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException($"Cannot refund payment. Current status: {Status}. Payment must be succeeded to be refunded.");

        Status = PaymentStatus.Refunded;

        // Raise domain event
        AddDomainEvent(new PaymentRefundedDomainEvent(
            Id,
            OrderId,
            Amount,
            DateTime.UtcNow));
    }
}
