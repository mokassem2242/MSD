using BuildingBlocks.Messaging;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Order.Domain.Aggregates.Order.Application.EventHandlers;

/// <summary>
/// Handles InventoryReserved: mark order as completed.
/// OrderCompleted is published via domain event in DomainEventDispatcher.
/// </summary>
public class InventoryReservedEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<InventoryReservedEventHandler> _logger;

    public InventoryReservedEventHandler(
        IOrderRepository orderRepository,
        ILogger<InventoryReservedEventHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(InventoryReserved integrationEvent)
    {
        _logger.LogInformation(
            "Received InventoryReserved for OrderId {OrderId}, ReservationId {ReservationId}",
            integrationEvent.OrderId,
            integrationEvent.ReservationId);

        var order = await _orderRepository.GetByIdAsync(integrationEvent.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for InventoryReserved", integrationEvent.OrderId);
            return;
        }

        if (order.Status == OrderStatus.Completed)
        {
            _logger.LogInformation("Order {OrderId} already completed (idempotent)", integrationEvent.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Paid)
        {
            _logger.LogWarning(
                "Order {OrderId} in status {Status}, cannot mark as completed",
                integrationEvent.OrderId,
                order.Status);
            return;
        }

        order.MarkAsCompleted();
        await _orderRepository.UpdateAsync(order);

        _logger.LogInformation("Order {OrderId} marked as Completed", integrationEvent.OrderId);
    }
}
