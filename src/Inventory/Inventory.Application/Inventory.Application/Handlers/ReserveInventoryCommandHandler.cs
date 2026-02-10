using FluentValidation;
using Inventory.Application.Commands;
using Inventory.Application.Ports;
using Inventory.Application.Results;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Handlers;

public class ReserveInventoryCommandHandler
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IValidator<ReserveInventoryCommand> _validator;
    private readonly ILogger<ReserveInventoryCommandHandler> _logger;

    public ReserveInventoryCommandHandler(
        IInventoryItemRepository inventoryItemRepository,
        IReservationRepository reservationRepository,
        IValidator<ReserveInventoryCommand> validator,
        ILogger<ReserveInventoryCommandHandler> logger)
    {
        _inventoryItemRepository = inventoryItemRepository ?? throw new ArgumentNullException(nameof(inventoryItemRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReserveInventoryResult> HandleAsync(ReserveInventoryCommand command)
    {
        var validationResult = await _validator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Idempotency: if we already have a reservation for this order, return success with existing data
        var existingReservation = await _reservationRepository.GetByOrderIdAsync(command.OrderId);
        if (existingReservation != null)
        {
            _logger.LogInformation("Reservation already exists for OrderId {OrderId}. ReservationId: {ReservationId}", command.OrderId, existingReservation.Id);
            var items = existingReservation.Lines.Select(l => new ReservedItem(l.ProductId, l.Quantity)).ToList();
            return new ReserveInventorySuccess(existingReservation.Id, items, existingReservation.ReservedAt);
        }

        var productIds = command.Items.Select(x => x.ProductId).Distinct().ToList();
        var inventoryItems = await _inventoryItemRepository.GetByProductIdsAsync(productIds);

        // Check availability and build failure list if any product is missing or insufficient
        var failedItems = new List<FailedItem>();
        foreach (var item in command.Items)
        {
            var inv = inventoryItems.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (inv == null)
            {
                failedItems.Add(new FailedItem(item.ProductId, item.Quantity, 0));
                continue;
            }

            if (inv.AvailableQuantity < item.Quantity)
                failedItems.Add(new FailedItem(item.ProductId, item.Quantity, inv.AvailableQuantity));
        }

        if (failedItems.Count > 0)
        {
            var reason = failedItems.Count == 1
                ? $"Insufficient stock for product {failedItems[0].ProductId}"
                : $"Insufficient stock for {failedItems.Count} product(s)";
            return new ReserveInventoryFailure(reason, failedItems);
        }

        // Reserve: update each inventory item and create reservation
        var lines = new List<(string ProductId, int Quantity)>();
        foreach (var item in command.Items)
        {
            var inv = inventoryItems.First(x => x.ProductId == item.ProductId);
            inv.Reserve(item.Quantity);
            await _inventoryItemRepository.UpdateAsync(inv);
            lines.Add((item.ProductId, item.Quantity));
        }

        var reservation = Reservation.Create(command.OrderId, lines);
        await _reservationRepository.AddAsync(reservation);

        _logger.LogInformation("Reserved inventory for OrderId {OrderId}. ReservationId: {ReservationId}", command.OrderId, reservation.Id);

        var reservedItems = reservation.Lines.Select(l => new ReservedItem(l.ProductId, l.Quantity)).ToList();
        return new ReserveInventorySuccess(reservation.Id, reservedItems, reservation.ReservedAt);
    }
}
