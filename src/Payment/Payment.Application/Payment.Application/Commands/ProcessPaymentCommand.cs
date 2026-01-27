namespace Payment.Application.Commands;

/// <summary>
/// Command to process a payment for an order.
/// Commands express intent - what we want to happen.
/// </summary>
public class ProcessPaymentCommand
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; }
    public decimal Amount { get; init; }

    public ProcessPaymentCommand(Guid orderId, string customerId, decimal amount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
    }
}
