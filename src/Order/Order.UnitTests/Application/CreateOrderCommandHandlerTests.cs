using Order.Domain.Aggregates.Order.Application.Commands;
using Order.Domain.Aggregates.Order.Application.Handlers;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Aggregates;
using Moq;
using Xunit;

namespace Order.Domain.Aggregates.Order.UnitTests.Application;

/// <summary>
/// Unit tests for CreateOrderCommandHandler.
/// Best candidate: command validation and orchestration with mocked repository.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly CreateOrderCommandHandler _sut;

    public CreateOrderCommandHandlerTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _sut = new CreateOrderCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CreateOrderCommandHandler(null!));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesOrderAndReturnsId()
    {
        var command = new CreateOrderCommand("customer-1", [
            new OrderItemCommand { ProductId = "prod-1", Quantity = 2, Price = 10m },
            new OrderItemCommand { ProductId = "prod-2", Quantity = 1, Price = 5m }
        ]);

        Guid capturedId = Guid.Empty;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order.Domain.Aggregates.Order>()))
            .Callback<Order.Domain.Aggregates.Order>(o => capturedId = o.Id)
            .Returns(Task.CompletedTask);

        var result = await _sut.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Order.Domain.Aggregates.Order>(o =>
            o.CustomerId == "customer-1" &&
            o.OrderItems.Count == 2 &&
            o.TotalAmount == 25m)), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyCustomerId_ThrowsArgumentException(string? customerId)
    {
        var command = new CreateOrderCommand(customerId!, [
            new OrderItemCommand { ProductId = "prod-1", Quantity = 1, Price = 10m }
        ]);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(command));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order.Domain.Aggregates.Order>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NullItems_ThrowsArgumentException()
    {
        var command = new CreateOrderCommand("customer-1", null!);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(command));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order.Domain.Aggregates.Order>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyItems_ThrowsArgumentException()
    {
        var command = new CreateOrderCommand("customer-1", []);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(command));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Order.Domain.Aggregates.Order>()), Times.Never);
    }
}
