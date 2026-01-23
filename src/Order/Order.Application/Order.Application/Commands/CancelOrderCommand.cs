namespace Order.Application.Commands;

/// <summary>
/// Command to cancel an order.
/// </summary>
public class CancelOrderCommand
{
    public Guid OrderId { get; init; }
    public string? Reason { get; init; }

    public CancelOrderCommand(Guid orderId, string? reason = null)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

