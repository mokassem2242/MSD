namespace BuildingBlocks.Messaging;

/// <summary>
/// Marker interface for integration events that cross module boundaries.
/// Integration events represent facts that have happened in one module and
/// can be consumed by other modules.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}

