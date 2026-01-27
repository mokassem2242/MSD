using BuildingBlocks.SharedKernel;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;

namespace Payment.Application.Ports;

/// <summary>
/// Repository interface for payment persistence.
/// This is a port in the hexagonal architecture - defines what the application needs,
/// not how it's implemented (that's in Infrastructure).
/// Extends the generic repository with payment-specific methods if needed.
/// </summary>
public interface IPaymentRepository : IRepository<PaymentAggregate, Guid>
{
    /// <summary>
    /// Gets a payment by order ID.
    /// Used for idempotency checks when processing payments.
    /// </summary>
    Task<PaymentAggregate?> GetByOrderIdAsync(Guid orderId);
}
