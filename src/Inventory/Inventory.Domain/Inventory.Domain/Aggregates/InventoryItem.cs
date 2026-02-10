using BuildingBlocks.SharedKernel;

namespace Inventory.Domain.Aggregates;

/// <summary>
/// Inventory item aggregate root. Tracks stock and reserved quantities per product.
/// </summary>
public class InventoryItem : AggregateRoot<Guid>
{
    public string ProductId { get; private set; }
    public int QuantityInStock { get; private set; }
    public int QuantityReserved { get; private set; }

    public int AvailableQuantity => QuantityInStock - QuantityReserved;

    private InventoryItem() : base()
    {
        ProductId = null!;
    }

    private InventoryItem(Guid id, string productId, int quantityInStock, int quantityReserved) : base(id)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId cannot be null or empty", nameof(productId));
        if (quantityInStock < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityInStock), "Quantity in stock cannot be negative");
        if (quantityReserved < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityReserved), "Quantity reserved cannot be negative");
        if (quantityReserved > quantityInStock)
            throw new ArgumentException("Quantity reserved cannot exceed quantity in stock");

        ProductId = productId;
        QuantityInStock = quantityInStock;
        QuantityReserved = quantityReserved;
    }

    public static InventoryItem Create(string productId, int quantityInStock)
    {
        return new InventoryItem(Guid.NewGuid(), productId, quantityInStock, 0);
    }

    /// <summary>
    /// Reserves the specified quantity. Returns true if successful.
    /// </summary>
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Reserve quantity must be positive");

        if (AvailableQuantity < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for product {ProductId}. Available: {AvailableQuantity}, Requested: {quantity}");

        QuantityReserved += quantity;
    }

    /// <summary>
    /// Releases (unreserves) the specified quantity.
    /// </summary>
    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Release quantity must be positive");

        if (QuantityReserved < quantity)
            throw new InvalidOperationException(
                $"Cannot release more than reserved for product {ProductId}. Reserved: {QuantityReserved}, Requested: {quantity}");

        QuantityReserved -= quantity;
    }

    /// <summary>
    /// Adjusts physical stock (e.g. after receiving goods). Use with care.
    /// </summary>
    public void AdjustStock(int delta)
    {
        if (QuantityInStock + delta < QuantityReserved)
            throw new InvalidOperationException(
                $"Cannot reduce stock below reserved quantity for product {ProductId}");
        QuantityInStock += delta;
    }
}
