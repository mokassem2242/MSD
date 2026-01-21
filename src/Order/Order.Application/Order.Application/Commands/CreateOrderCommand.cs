namespace Order.Application.Commands;

/// <summary>
/// Command to create a new order.
/// Commands express intent - what we want to happen.
/// </summary>
public class CreateOrderCommand
{
    public string CustomerId { get; init; }
    public List<OrderItemCommand> Items { get; init; } = new();

    public CreateOrderCommand(string customerId, List<OrderItemCommand> items)
    {
        CustomerId = customerId;
        Items = items;
    }
}

/// <summary>
/// Represents an order item in the command.
/// </summary>
public class OrderItemCommand
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

