using BuildingBlocks.SharedKernel;

namespace Order.Domain.Aggregates.Order.Domain.ValueObjects;

/// <summary>
/// Value object representing an item in an order.
/// Immutable and compared by value (all properties).
/// </summary>
public class OrderItem : ValueObject
{
    public string ProductId { get; }
    public int Quantity { get; }
    public decimal Price { get; }

    public OrderItem(string productId, int quantity, decimal price)
    {
     

        ArgumentNullException.ThrowIfNullOrWhiteSpace(productId);
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        ProductId = productId;
        Quantity = quantity;
        Price = price;
    }

    /// <summary>
    /// Calculates the total price for this order item (quantity * price).
    /// </summary>
    public decimal GetTotalPrice() => Quantity * Price;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return Quantity;
        yield return Price;
    }
}

