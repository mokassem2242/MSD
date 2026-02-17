using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using Order.Domain.Aggregates.Order.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Order.Domain.Aggregates.Order.Application.EventHandlers;

/// <summary>
/// Handles InventoryFailed: request refund (publish RefundRequested).
/// When Payment service processes refund it will publish PaymentRefunded; Order then cancels in PaymentRefundedEventHandler.
/// </summary>
public class InventoryFailedEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<InventoryFailedEventHandler> _logger;

    public InventoryFailedEventHandler(
        IOrderRepository orderRepository,
        IEventBus eventBus,
        ILogger<InventoryFailedEventHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(InventoryFailed integrationEvent)
    {
        _logger.LogInformation(
            "Received InventoryFailed for OrderId {OrderId}, Reason: {Reason}",
            integrationEvent.OrderId,
            integrationEvent.FailureReason);

        var order = await _orderRepository.GetByIdAsync(integrationEvent.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for InventoryFailed", integrationEvent.OrderId);
            return;
        }

        var refundRequested = new RefundRequested(
            integrationEvent.OrderId,
            $"Inventory failed: {integrationEvent.FailureReason}",
            DateTime.UtcNow);
        await _eventBus.PublishAsync(refundRequested);

        _logger.LogInformation(
            "Published RefundRequested for OrderId {OrderId} (compensation for inventory failure)",
            integrationEvent.OrderId);
    }
}
