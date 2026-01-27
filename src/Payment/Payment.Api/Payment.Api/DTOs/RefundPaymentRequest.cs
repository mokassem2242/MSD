namespace Payment.Api.DTOs;

/// <summary>
/// Request DTO for refunding a payment.
/// </summary>
public class RefundPaymentRequest
{
    public Guid PaymentId { get; init; }
}
