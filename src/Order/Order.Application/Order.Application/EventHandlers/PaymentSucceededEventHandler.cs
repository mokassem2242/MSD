using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Enums;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;
using Microsoft.Extensions.Logging;

namespace Order.Domain.Aggregates.Order.Application.EventHandlers;

/// <summary>
/// Handles PaymentSucceeded: mark order as paid and publish OrderInventoryRequested.
/// </summary>
public class PaymentSucceededEventHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentSucceededEventHandler> _logger;

    public PaymentSucceededEventHandler(
        IOrderRepository orderRepository,
        IEventBus eventBus,
        ILogger<PaymentSucceededEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(logger);

        _orderRepository = orderRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentSucceeded integrationEvent)
    {
        _logger.LogInformation(
            "Received PaymentSucceeded for OrderId {OrderId}, PaymentId {PaymentId}",
            integrationEvent.OrderId,
            integrationEvent.PaymentId);

        var order = await _orderRepository.GetByIdAsync(integrationEvent.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for PaymentSucceeded", integrationEvent.OrderId);
            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation(
                "Order {OrderId} already in status {Status}, skipping (idempotent)",
                integrationEvent.OrderId,
                order.Status);
            return;
        }

        order.MarkAsPaid();
        await _orderRepository.UpdateAsync(order);

        var items = order.OrderItems
            .Select(item => new RequestedItemDto { ProductId = item.ProductId, Quantity = item.Quantity })
            .ToList();
        var orderInventoryRequested = new OrderInventoryRequested(
            order.Id,
            items,
            DateTime.UtcNow);
        await _eventBus.PublishAsync(orderInventoryRequested);

        _logger.LogInformation(
            "Order {OrderId} marked as Paid and OrderInventoryRequested published",
            integrationEvent.OrderId);
    }
}
