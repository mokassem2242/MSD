using BuildingBlocks.Messaging;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Order.Domain.Aggregates.Order.Application.EventHandlers;

/// <summary>
/// Handles PaymentFailed: cancel the order.
/// </summary>
public class PaymentFailedEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(
        IOrderRepository orderRepository,
        ILogger<PaymentFailedEventHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(PaymentFailed integrationEvent)
    {
        _logger.LogInformation(
            "Received PaymentFailed for OrderId {OrderId}, Reason: {Reason}",
            integrationEvent.OrderId,
            integrationEvent.FailureReason);

        var order = await _orderRepository.GetByIdAsync(integrationEvent.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for PaymentFailed", integrationEvent.OrderId);
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} already cancelled (idempotent)", integrationEvent.OrderId);
            return;
        }

        if (order.Status == OrderStatus.Completed)
        {
            _logger.LogWarning("Order {OrderId} already completed, cannot cancel", integrationEvent.OrderId);
            return;
        }

        order.Cancel(integrationEvent.FailureReason);
        await _orderRepository.UpdateAsync(order);

        _logger.LogInformation("Order {OrderId} cancelled due to payment failure", integrationEvent.OrderId);
    }
}
