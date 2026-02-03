using Order.Domain.Aggregates.Order.Domain.Enums;

namespace Order.Domain.Aggregates.Order.Api.DTOs;

/// <summary>
/// Response DTO for order data.
/// </summary>
public class OrderResponse
{
    public Guid Id { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public List<OrderItemResponse> Items { get; init; } = new();
    public OrderStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Represents an order item in the response.
/// </summary>
public class OrderItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal TotalPrice { get; init; }
}

