using Order.Domain.Aggregates.Order.Domain.ValueObjects;
using Xunit;

namespace Order.Domain.Aggregates.Order.UnitTests.Domain;

/// <summary>
/// Unit tests for the OrderItem value object.
/// Best candidate: pure domain logic, validation, and calculation.
/// </summary>
public class OrderItemTests
{
    [Fact]
    public void Constructor_ValidInputs_CreatesOrderItem()
    {
        var item = new OrderItem("prod-1", 2, 10.50m);

        Assert.Equal("prod-1", item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(10.50m, item.Price);
    }

    [Fact]
    public void GetTotalPrice_ReturnsQuantityTimesPrice()
    {
        var item = new OrderItem("prod-1", 3, 5.00m);

        Assert.Equal(15.00m, item.GetTotalPrice());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyProductId_ThrowsArgumentException(string? productId)
    {
        Assert.Throws<ArgumentException>(() => new OrderItem(productId!, 1, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_QuantityZeroOrNegative_ThrowsArgumentException(int quantity)
    {
        Assert.Throws<ArgumentException>(() => new OrderItem("prod-1", quantity, 10m));
    }

    [Fact]
    public void Constructor_NegativePrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new OrderItem("prod-1", 1, -5m));
    }

    [Fact]
    public void Constructor_ZeroPrice_IsAllowed()
    {
        var item = new OrderItem("prod-1", 1, 0m);
        Assert.Equal(0m, item.GetTotalPrice());
    }
}
