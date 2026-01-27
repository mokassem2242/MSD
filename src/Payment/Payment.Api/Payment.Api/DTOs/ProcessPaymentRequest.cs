namespace Payment.Api.DTOs;

/// <summary>
/// Request DTO for processing a payment.
/// </summary>
public class ProcessPaymentRequest
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
