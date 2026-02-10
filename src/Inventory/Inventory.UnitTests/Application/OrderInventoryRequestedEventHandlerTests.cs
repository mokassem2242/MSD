using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using FluentValidation;
using Inventory.Application.Handlers;
using Inventory.Application.Ports;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Unit tests for OrderInventoryRequestedEventHandler.
/// </summary>
public class OrderInventoryRequestedEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<OrderInventoryRequestedEventHandler>> _loggerMock;
    private readonly OrderInventoryRequestedEventHandler _sut;

    private static ReserveInventoryCommandHandler CreateReserveHandler(
        IInventoryItemRepository inventoryRepo,
        IReservationRepository reservationRepo,
        IValidator<Inventory.Application.Commands.ReserveInventoryCommand>? validator = null)
    {
        var validatorMock = new Mock<IValidator<Inventory.Application.Commands.ReserveInventoryCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Inventory.Application.Commands.ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        return new ReserveInventoryCommandHandler(
            inventoryRepo,
            reservationRepo,
            validator ?? validatorMock.Object,
            new Mock<ILogger<ReserveInventoryCommandHandler>>().Object);
    }

    public OrderInventoryRequestedEventHandlerTests()
    {
        var inventoryRepoMock = new Mock<IInventoryItemRepository>();
        var reservationRepoMock = new Mock<IReservationRepository>();
        var reserveHandler = CreateReserveHandler(inventoryRepoMock.Object, reservationRepoMock.Object);

        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<OrderInventoryRequestedEventHandler>>();
        _sut = new OrderInventoryRequestedEventHandler(reserveHandler, _eventBusMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullReserveHandler_ThrowsArgumentNullException()
    {
        var eventBus = new Mock<IEventBus>().Object;
        var logger = new Mock<ILogger<OrderInventoryRequestedEventHandler>>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            new OrderInventoryRequestedEventHandler(null!, eventBus, logger));
    }

    [Fact]
    public void Constructor_NullEventBus_ThrowsArgumentNullException()
    {
        var inventoryRepo = new Mock<IInventoryItemRepository>().Object;
        var reservationRepo = new Mock<IReservationRepository>().Object;
        var reserveHandler = CreateReserveHandler(inventoryRepo, reservationRepo);
        var logger = new Mock<ILogger<OrderInventoryRequestedEventHandler>>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            new OrderInventoryRequestedEventHandler(reserveHandler, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var inventoryRepo = new Mock<IInventoryItemRepository>().Object;
        var reservationRepo = new Mock<IReservationRepository>().Object;
        var reserveHandler = CreateReserveHandler(inventoryRepo, reservationRepo);
        var eventBus = new Mock<IEventBus>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            new OrderInventoryRequestedEventHandler(reserveHandler, eventBus, null!));
    }

    [Fact]
    public async Task HandleAsync_WhenReserveSucceeds_PublishesInventoryReserved()
    {
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var reservedAt = DateTime.UtcNow;
        var inventoryRepoMock = new Mock<IInventoryItemRepository>();
        var reservationRepoMock = new Mock<IReservationRepository>();
        var item = Inventory.Domain.Aggregates.InventoryItem.Create("prod-1", 10);
        reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Inventory.Domain.Entities.Reservation?)null);
        inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new[] { item });
        reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Inventory.Domain.Entities.Reservation>()))
            .Callback<Inventory.Domain.Entities.Reservation>(r => { /* reservation created */ })
            .Returns(Task.CompletedTask);
        inventoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Inventory.Domain.Aggregates.InventoryItem>())).Returns(Task.CompletedTask);

        var reserveHandler = CreateReserveHandler(inventoryRepoMock.Object, reservationRepoMock.Object);
        var handler = new OrderInventoryRequestedEventHandler(reserveHandler, _eventBusMock.Object, _loggerMock.Object);

        var integrationEvent = new OrderInventoryRequested(orderId, [new RequestedItemDto { ProductId = "prod-1", Quantity = 2 }], DateTime.UtcNow);

        await handler.HandleAsync(integrationEvent);

        _eventBusMock.Verify(
            e => e.PublishAsync(It.Is<InventoryReserved>(ev =>
                ev.OrderId == orderId &&
                ev.Items.Count == 1 &&
                ev.Items[0].ProductId == "prod-1" &&
                ev.Items[0].Quantity == 2)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenReserveFails_PublishesInventoryFailed()
    {
        var orderId = Guid.NewGuid();
        var inventoryRepoMock = new Mock<IInventoryItemRepository>();
        var reservationRepoMock = new Mock<IReservationRepository>();
        var item = Inventory.Domain.Aggregates.InventoryItem.Create("prod-1", 1);
        reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Inventory.Domain.Entities.Reservation?)null);
        inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new[] { item });

        var reserveHandler = CreateReserveHandler(inventoryRepoMock.Object, reservationRepoMock.Object);
        var handler = new OrderInventoryRequestedEventHandler(reserveHandler, _eventBusMock.Object, _loggerMock.Object);

        var integrationEvent = new OrderInventoryRequested(orderId, [new RequestedItemDto { ProductId = "prod-1", Quantity = 5 }], DateTime.UtcNow);

        await handler.HandleAsync(integrationEvent);

        _eventBusMock.Verify(
            e => e.PublishAsync(It.Is<InventoryFailed>(ev =>
                ev.OrderId == orderId &&
                ev.FailedItems.Count == 1 &&
                ev.FailedItems[0].ProductId == "prod-1" &&
                ev.FailedItems[0].RequestedQuantity == 5 &&
                ev.FailedItems[0].AvailableQuantity == 1)),
            Times.Once);
    }
}
