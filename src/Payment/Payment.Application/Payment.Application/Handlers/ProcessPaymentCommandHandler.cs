using Payment.Application.Commands;
using Payment.Application.Ports;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Handlers;

/// <summary>
/// Handles the ProcessPaymentCommand by creating a payment aggregate and processing it.
/// Domain events are automatically published in SaveChangesAsync.
/// </summary>
public class ProcessPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;
    private readonly Random _random = new();

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the process payment command.
    /// For demo purposes, payment processing randomly succeeds or fails.
    /// In production, this would call a real payment gateway.
    /// </summary>
    /// <param name="command">The command containing payment details</param>
    /// <returns>The ID of the created payment</returns>
    public async Task<Guid> HandleAsync(ProcessPaymentCommand command)
    {
        // Validate command
        if (command.OrderId == Guid.Empty)
            throw new ArgumentException("OrderId is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.CustomerId))
            throw new ArgumentException("CustomerId is required", nameof(command));

        if (command.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(command));

        // Check idempotency - if payment already exists for this order, return existing payment
        var existingPayment = await _paymentRepository.GetByOrderIdAsync(command.OrderId);
        if (existingPayment != null)
        {
            _logger.LogInformation(
                "Payment already exists for OrderId {OrderId}. PaymentId: {PaymentId}, Status: {Status}",
                command.OrderId,
                existingPayment.Id,
                existingPayment.Status);

            return existingPayment.Id;
        }

        // Create payment aggregate using factory method
        var payment = PaymentAggregate.Create(command.OrderId, command.CustomerId, command.Amount);

        _logger.LogInformation(
            "Processing payment for OrderId {OrderId}, Amount: {Amount}",
            command.OrderId,
            command.Amount);

        // Simulate payment processing (random success/failure for demo)
        // In production, this would call a real payment gateway
        var success = _random.Next(0, 100) < 80; // 80% success rate for demo

        if (success)
        {
            payment.MarkAsSucceeded();
            _logger.LogInformation(
                "Payment succeeded for OrderId {OrderId}. PaymentId: {PaymentId}",
                command.OrderId,
                payment.Id);
        }
        else
        {
            var failureReason = "Simulated payment gateway failure";
            payment.MarkAsFailed(failureReason);
            _logger.LogWarning(
                "Payment failed for OrderId {OrderId}. PaymentId: {PaymentId}, Reason: {Reason}",
                command.OrderId,
                payment.Id,
                failureReason);
        }

        // Save to repository
        // Domain events will be automatically published in SaveChangesAsync
        await _paymentRepository.AddAsync(payment);

        return payment.Id;
    }
}
