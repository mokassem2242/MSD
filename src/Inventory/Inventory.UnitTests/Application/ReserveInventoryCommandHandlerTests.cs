using FluentValidation;
using Inventory.Application.Commands;
using Inventory.Application.Handlers;
using Inventory.Application.Ports;
using Inventory.Application.Results;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Inventory.UnitTests.Application;

/// <summary>
/// Unit tests for ReserveInventoryCommandHandler.
/// </summary>
public class ReserveInventoryCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _inventoryRepoMock;
    private readonly Mock<IReservationRepository> _reservationRepoMock;
    private readonly Mock<IValidator<ReserveInventoryCommand>> _validatorMock;
    private readonly Mock<ILogger<ReserveInventoryCommandHandler>> _loggerMock;
    private readonly ReserveInventoryCommandHandler _sut;

    public ReserveInventoryCommandHandlerTests()
    {
        _inventoryRepoMock = new Mock<IInventoryItemRepository>();
        _reservationRepoMock = new Mock<IReservationRepository>();
        _validatorMock = new Mock<IValidator<ReserveInventoryCommand>>();
        _loggerMock = new Mock<ILogger<ReserveInventoryCommandHandler>>();

        // Default: validation passes
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _sut = new ReserveInventoryCommandHandler(
            _inventoryRepoMock.Object,
            _reservationRepoMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullInventoryRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReserveInventoryCommandHandler(null!, _reservationRepoMock.Object, _validatorMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullReservationRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReserveInventoryCommandHandler(_inventoryRepoMock.Object, null!, _validatorMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReserveInventoryCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReserveInventoryCommandHandler(_inventoryRepoMock.Object, _reservationRepoMock.Object, _validatorMock.Object, null!));
    }

    [Fact]
    public async Task HandleAsync_EmptyOrderId_ThrowsValidationException()
    {
        var command = new ReserveInventoryCommand
        {
            OrderId = Guid.Empty,
            Items = [new ReserveInventoryItem("prod-1", 1)]
        };
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                [new FluentValidation.Results.ValidationFailure("OrderId", "OrderId is required.")]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.HandleAsync(command));
        Assert.Contains(ex.Errors, f => f.PropertyName == "OrderId");
    }

    [Fact]
    public async Task HandleAsync_NullItems_ThrowsValidationException()
    {
        var command = new ReserveInventoryCommand
        {
            OrderId = Guid.NewGuid(),
            Items = null!
        };
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                [new FluentValidation.Results.ValidationFailure("Items", "Items are required.")]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.HandleAsync(command));
        Assert.Contains(ex.Errors, f => f.PropertyName == "Items");
    }

    [Fact]
    public async Task HandleAsync_EmptyItems_ThrowsValidationException()
    {
        var command = new ReserveInventoryCommand
        {
            OrderId = Guid.NewGuid(),
            Items = []
        };
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                [new FluentValidation.Results.ValidationFailure("Items", "At least one item is required.")]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.HandleAsync(command));
        Assert.Contains(ex.Errors, f => f.PropertyName == "Items");
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_InvokesValidator()
    {
        var orderId = Guid.NewGuid();
        var command = new ReserveInventoryCommand
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-1", 1)]
        };
        var existingReservation = Reservation.Create(orderId, [("prod-1", 1)]);
        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(existingReservation);

        await _sut.HandleAsync(command);

        _validatorMock.Verify(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingReservation_ReturnsSuccessWithExistingReservationId()
    {
        var orderId = Guid.NewGuid();
        var existingReservation = Reservation.Create(orderId, [("prod-1", 2)]);

        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(existingReservation);

        var command = new ReserveInventoryCommand
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-1", 2)]
        };

        var result = await _sut.HandleAsync(command);

        var success = Assert.IsType<ReserveInventorySuccess>(result);
        Assert.Equal(existingReservation.Id, success.ReservationId);
        Assert.Single(success.Items);
        Assert.Equal("prod-1", success.Items[0].ProductId);
        Assert.Equal(2, success.Items[0].Quantity);
        Assert.Equal(existingReservation.ReservedAt, success.ReservedAt);
        _inventoryRepoMock.Verify(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReservesAndReturnsSuccess()
    {
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create("prod-1", 10);
        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Reservation?)null);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { item });
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _inventoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<InventoryItem>())).Returns(Task.CompletedTask);

        var command = new ReserveInventoryCommand
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-1", 3)]
        };

        var result = await _sut.HandleAsync(command);

        var success = Assert.IsType<ReserveInventorySuccess>(result);
        Assert.NotEqual(Guid.Empty, success.ReservationId);
        Assert.Single(success.Items);
        Assert.Equal("prod-1", success.Items[0].ProductId);
        Assert.Equal(3, success.Items[0].Quantity);
        Assert.Equal(3, item.QuantityReserved);
        _reservationRepoMock.Verify(r => r.AddAsync(It.Is<Reservation>(res =>
            res.OrderId == orderId && res.Lines.Count == 1 && res.Lines[0].Quantity == 3)), Times.Once);
        _inventoryRepoMock.Verify(r => r.UpdateAsync(item), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InsufficientStock_ReturnsFailure()
    {
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create("prod-1", 2);
        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Reservation?)null);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { item });

        var command = new ReserveInventoryCommand
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-1", 5)]
        };



        var result = await _sut.HandleAsync(command);

        var failure = Assert.IsType<ReserveInventoryFailure>(result);
        Assert.Contains("Insufficient", failure.Reason);
        Assert.Single(failure.FailedItems);
        Assert.Equal("prod-1", failure.FailedItems[0].ProductId);
        Assert.Equal(5, failure.FailedItems[0].RequestedQuantity);
        Assert.Equal(2, failure.FailedItems[0].AvailableQuantity);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
        _inventoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<InventoryItem>()), Times.Never);
    }

    // ----- Implement the following two tests yourself -----

    /// <summary>
    /// TODO: Implement this test yourself.
    /// When the command requests a product that does not exist in inventory (GetByProductIdsAsync
    /// returns no item for that product), the handler should return ReserveInventoryFailure
    /// with a FailedItem where AvailableQuantity is 0.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ProductNotInInventory_ReturnsFailureWithZeroAvailable()
    {
        //arrange
        var orderId = Guid.NewGuid();

        var command = new ReserveInventoryCommand()
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-2", 5)]
        };
        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Reservation?)null);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<InventoryItem>().AsReadOnly());

        //act
        var result = await _sut.HandleAsync(command);


        var failure = Assert.IsType<ReserveInventoryFailure>(result);
        Assert.Contains("Insufficient", failure.Reason);
        Assert.Equal(0, failure.FailedItems[0].AvailableQuantity);
        _reservationRepoMock.Verify(r => r.GetByOrderIdAsync(orderId), Times.Once);
        _inventoryRepoMock.Verify(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
    }
    /// <summary>
    /// TODO: Implement this test yourself.
    /// When multiple items are requested and one has insufficient stock, the result should be
    /// ReserveInventoryFailure with multiple FailedItems (or at least the failing one), and
    /// no reservation should be created (AddAsync never called).
    /// </summary>
    [Fact]
    public async Task HandleAsync_MultipleItemsOneInsufficient_ReturnsFailureAndNoReservation()
    {
        //arrange
        var orderId = Guid.NewGuid();
        var item_1 = InventoryItem.Create("prod-1", 10);
        var item_2 = InventoryItem.Create("prod-2", 10);
        var item_3 = InventoryItem.Create("prod-3", 3);

        var command = new ReserveInventoryCommand()
        {
            OrderId = orderId,
            Items = [new ReserveInventoryItem("prod-1", 5), new ReserveInventoryItem("prod-2", 5), new ReserveInventoryItem("prod-3", 5)]
        };
        _reservationRepoMock.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Reservation?)null);
        _inventoryRepoMock.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<InventoryItem>() {
        item_1, item_2, item_3
        });

        //act
        var result = await _sut.HandleAsync(command);

        var failure = Assert.IsType<ReserveInventoryFailure>(result);
        Assert.Contains("Insufficient", failure.Reason);

        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);


    }
}
