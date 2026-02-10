using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using Inventory.Application.Commands;
using Inventory.Application.Handlers;
using Inventory.Application.Results;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Handlers;

/// <summary>
/// Consumes OrderInventoryRequested integration events and reserves inventory.
/// Publishes InventoryReserved or InventoryFailed based on result.
/// </summary>
public class OrderInventoryRequestedEventHandler
{
    private readonly ReserveInventoryCommandHandler _reserveHandler;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderInventoryRequestedEventHandler> _logger;

    public OrderInventoryRequestedEventHandler(
        ReserveInventoryCommandHandler reserveHandler,
        IEventBus eventBus,
        ILogger<OrderInventoryRequestedEventHandler> logger)
    {
        _reserveHandler = reserveHandler ?? throw new ArgumentNullException(nameof(reserveHandler));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderInventoryRequested integrationEvent)
    {
        _logger.LogInformation(
            "Received OrderInventoryRequested for OrderId {OrderId}, Items: {ItemCount}",
            integrationEvent.OrderId,
            integrationEvent.Items.Count);

        var command = new ReserveInventoryCommand
        {
            OrderId = integrationEvent.OrderId,
            Items = integrationEvent.Items
                .Select(x => new ReserveInventoryItem(x.ProductId, x.Quantity))
                .ToList()
        };

        var result = await _reserveHandler.HandleAsync(command);

        if (result is ReserveInventorySuccess success)
        {
            var @event = new InventoryReserved(
                success.ReservationId,
                integrationEvent.OrderId,
                success.Items.Select(x => new ReservedItemDto { ProductId = x.ProductId, Quantity = x.Quantity }).ToList(),
                success.ReservedAt);
            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("Published InventoryReserved for OrderId {OrderId}, ReservationId {ReservationId}", integrationEvent.OrderId, success.ReservationId);
        }
        else if (result is ReserveInventoryFailure failure)
        {
            var @event = new InventoryFailed(
                integrationEvent.OrderId,
                failure.Reason,
                failure.FailedItems.Select(x => new FailedItemDto { ProductId = x.ProductId, RequestedQuantity = x.RequestedQuantity, AvailableQuantity = x.AvailableQuantity }).ToList(),
                DateTime.UtcNow);
            await _eventBus.PublishAsync(@event);
            _logger.LogWarning("Published InventoryFailed for OrderId {OrderId}: {Reason}", integrationEvent.OrderId, failure.Reason);
        }
    }
}
