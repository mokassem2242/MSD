namespace Order.Api.DTOs;

/// <summary>
/// Request DTO for creating a new order.
/// </summary>
public class CreateOrderRequest
{
    public required string CustomerId { get; init; }
    public required List<OrderItemRequest> Items { get; init; }
}

/// <summary>
/// Represents an order item in the request.
/// </summary>
public class OrderItemRequest
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

