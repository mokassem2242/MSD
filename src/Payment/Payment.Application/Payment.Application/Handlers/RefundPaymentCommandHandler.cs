using Payment.Application.Commands;
using Payment.Application.Ports;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Handlers;

/// <summary>
/// Handles the RefundPaymentCommand by refunding a payment.
/// Domain events are automatically published in SaveChangesAsync.
/// </summary>
public class RefundPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the refund payment command.
    /// </summary>
    /// <param name="command">The command containing payment ID</param>
    public async Task HandleAsync(RefundPaymentCommand command)
    {
        // Validate command
        if (command.PaymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required", nameof(command));

        // Load payment
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId);
        if (payment == null)
            throw new InvalidOperationException($"Payment with ID {command.PaymentId} not found");

        _logger.LogInformation(
            "Refunding payment {PaymentId} for OrderId {OrderId}, Amount: {Amount}",
            payment.Id,
            payment.OrderId,
            payment.Amount);

        // Refund payment (validates status internally)
        payment.Refund();

        // Save to repository
        // Domain events will be automatically published in SaveChangesAsync
        await _paymentRepository.UpdateAsync(payment);

        _logger.LogInformation(
            "Payment {PaymentId} refunded successfully",
            payment.Id);
    }
}
