using Inventory.Domain.Aggregates;
using Xunit;

namespace Inventory.UnitTests.Domain;

/// <summary>
/// Unit tests for InventoryItem aggregate root.
/// </summary>
public class InventoryItemTests
{
    [Fact]
    public void Create_ValidInputs_CreatesItemWithCorrectState()
    {
        var item = InventoryItem.Create("prod-1", 10);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("prod-1", item.ProductId);
        Assert.Equal(10, item.QuantityInStock);
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(10, item.AvailableQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrEmptyProductId_ThrowsArgumentException(string? productId)
    {
        Assert.Throws<ArgumentException>(() => InventoryItem.Create(productId!, 5));
    }

    [Fact]
    public void Create_NegativeQuantityInStock_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InventoryItem.Create("prod-1", -1));
    }

    [Fact]
    public void Reserve_ValidQuantity_IncreasesQuantityReserved()
    {
        var item = InventoryItem.Create("prod-1", 10);

        item.Reserve(3);

        Assert.Equal(3, item.QuantityReserved);
        Assert.Equal(7, item.AvailableQuantity);
    }

    [Fact]
    public void Reserve_QuantityEqualToAvailable_Succeeds()
    {
        var item = InventoryItem.Create("prod-1", 5);
        item.Reserve(5);
        Assert.Equal(5, item.QuantityReserved);
        Assert.Equal(0, item.AvailableQuantity);
    }

    [Fact]
    public void Reserve_QuantityExceedsAvailable_ThrowsInvalidOperationException()
    {
        var item = InventoryItem.Create("prod-1", 5);
        var ex = Assert.Throws<InvalidOperationException>(() => item.Reserve(6));
        Assert.Contains("Insufficient", ex.Message);
        Assert.Equal(0, item.QuantityReserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_ZeroOrNegativeQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var item = InventoryItem.Create("prod-1", 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.Reserve(quantity));
    }

    [Fact]
    public void Release_ValidQuantity_DecreasesQuantityReserved()
    {
        var item = InventoryItem.Create("prod-1", 10);
        item.Reserve(5);
        item.Release(2);
        Assert.Equal(3, item.QuantityReserved);
        Assert.Equal(7, item.AvailableQuantity);
    }

    [Fact]
    public void Release_QuantityExceedsReserved_ThrowsInvalidOperationException()
    {
        var item = InventoryItem.Create("prod-1", 10);
        item.Reserve(3);
        var ex = Assert.Throws<InvalidOperationException>(() => item.Release(4));
        Assert.Contains("Cannot release more", ex.Message);
    }

    [Fact]
    public void AdjustStock_PositiveDelta_IncreasesQuantityInStock()
    {
        var item = InventoryItem.Create("prod-1", 10);
        item.AdjustStock(5);
        Assert.Equal(15, item.QuantityInStock);
    }

    [Fact]
    public void AdjustStock_NegativeDeltaThatStaysAboveReserved_Succeeds()
    {
        var item = InventoryItem.Create("prod-1", 10);
        item.Reserve(3);
        item.AdjustStock(-2);
        Assert.Equal(8, item.QuantityInStock);
        Assert.Equal(3, item.QuantityReserved);
    }

    [Fact]
    public void AdjustStock_NegativeDeltaBelowReserved_ThrowsInvalidOperationException()
    {
        var item = InventoryItem.Create("prod-1", 10);
        item.Reserve(5);
        var ex = Assert.Throws<InvalidOperationException>(() => item.AdjustStock(-7));
        Assert.Contains("Cannot reduce stock below reserved", ex.Message);
    }
}
