namespace Payment.Application.Commands;

/// <summary>
/// Command to refund a payment.
/// Commands express intent - what we want to happen.
/// </summary>
public class RefundPaymentCommand
{
    public Guid PaymentId { get; init; }

    public RefundPaymentCommand(Guid paymentId)
    {
        PaymentId = paymentId;
    }
}
