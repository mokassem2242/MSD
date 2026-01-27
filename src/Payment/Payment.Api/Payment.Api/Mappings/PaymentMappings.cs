using Payment.Api.DTOs;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;

namespace Payment.Api.Mappings;

/// <summary>
/// Extension methods for mapping Payment domain objects to DTOs.
/// </summary>
public static class PaymentMappings
{
    /// <summary>
    /// Maps a Payment aggregate to a PaymentResponse DTO.
    /// </summary>
    public static PaymentResponse ToResponse(this PaymentAggregate payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            CustomerId = payment.CustomerId,
            Amount = payment.Amount,
            Status = payment.Status,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt,
            FailureReason = payment.FailureReason
        };
    }
}
