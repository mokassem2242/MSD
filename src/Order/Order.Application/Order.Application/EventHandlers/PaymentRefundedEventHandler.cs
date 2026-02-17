using BuildingBlocks.Messaging;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Order.Domain.Aggregates.Order.Application.EventHandlers;

/// <summary>
/// Handles PaymentRefunded: cancel the order (saga compensation complete).
/// </summary>
public class PaymentRefundedEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentRefundedEventHandler> _logger;

    public PaymentRefundedEventHandler(
        IOrderRepository orderRepository,
        ILogger<PaymentRefundedEventHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(PaymentRefunded integrationEvent)
    {
        _logger.LogInformation(
            "Received PaymentRefunded for OrderId {OrderId}, RefundId {RefundId}",
            integrationEvent.OrderId,
            integrationEvent.RefundId);

        var order = await _orderRepository.GetByIdAsync(integrationEvent.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for PaymentRefunded", integrationEvent.OrderId);
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

        order.Cancel($"Payment refunded: {integrationEvent.RefundId}");
        await _orderRepository.UpdateAsync(order);

        _logger.LogInformation("Order {OrderId} cancelled after refund", integrationEvent.OrderId);
    }
}
