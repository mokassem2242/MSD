namespace Payment.Domain.Enums;

/// <summary>
/// Represents the current status of a payment in its lifecycle.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment has been created and is awaiting processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been successfully processed.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Payment processing has failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment has been refunded (compensation for failed inventory or cancellation).
    /// </summary>
    Refunded = 3
}
