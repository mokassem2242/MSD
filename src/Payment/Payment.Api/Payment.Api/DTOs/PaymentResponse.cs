using Payment.Domain.Enums;

namespace Payment.Api.DTOs;

/// <summary>
/// Response DTO for payment data.
/// </summary>
public class PaymentResponse
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? FailureReason { get; init; }
}
