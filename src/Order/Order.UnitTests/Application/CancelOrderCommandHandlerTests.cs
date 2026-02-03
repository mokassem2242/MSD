using Order.Domain.Aggregates.Order.Application.Commands;
using Order.Domain.Aggregates.Order.Application.Handlers;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Aggregates;
using Order.Domain.Aggregates.Order.Domain.ValueObjects;
using Moq;
using Xunit;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.UnitTests.Application;

/// <summary>
/// Unit tests for CancelOrderCommandHandler.
/// Best candidate: not-found handling and cancel flow with mocked repository.
/// </summary>
public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly CancelOrderCommandHandler _sut;

    public CancelOrderCommandHandlerTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _sut = new CancelOrderCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CancelOrderCommandHandler(null!));
    }

    [Fact]
    public async Task HandleAsync_OrderExists_CancelsAndUpdates()
    {
        var orderId = Guid.NewGuid(); 
        var order = OrderAggregate.Create("customer-1", [new OrderItem("prod-1", 1, 10m)]);
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var command = new CancelOrderCommand(orderId, "Refund requested");

        await _sut.HandleAsync(command);

        Assert.Equal(Order.Domain.Enums.OrderStatus.Cancelled, order.Status);
        _repositoryMock.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var orderId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((OrderAggregate?)null);

        var command = new CancelOrderCommand(orderId, null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
        Assert.Contains(orderId.ToString(), ex.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<OrderAggregate>()), Times.Never);
    }
}
