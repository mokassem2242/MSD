using BuildingBlocks.Messaging;
using Payment.Application.Commands;
using Payment.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Handlers;

/// <summary>
/// Consumes RefundRequested integration events (from Order saga) and triggers refund.
/// </summary>
public class RefundRequestedEventHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly RefundPaymentCommandHandler _refundHandler;
    private readonly ILogger<RefundRequestedEventHandler> _logger;

    public RefundRequestedEventHandler(
        IPaymentRepository paymentRepository,
        RefundPaymentCommandHandler refundHandler,
        ILogger<RefundRequestedEventHandler> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _refundHandler = refundHandler ?? throw new ArgumentNullException(nameof(refundHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(RefundRequested integrationEvent)
    {
        _logger.LogInformation(
            "Received RefundRequested for OrderId {OrderId}, Reason: {Reason}",
            integrationEvent.OrderId,
            integrationEvent.Reason);

        var payment = await _paymentRepository.GetByOrderIdAsync(integrationEvent.OrderId);
        if (payment == null)
        {
            _logger.LogWarning("No payment found for OrderId {OrderId}, skipping refund", integrationEvent.OrderId);
            return;
        }

        await _refundHandler.HandleAsync(new RefundPaymentCommand(payment.Id));
    }
}
