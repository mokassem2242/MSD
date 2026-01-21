namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Marker interface for aggregate roots.
/// Only aggregate roots should have repositories.
/// This interface enforces the DDD rule that repositories can only be created for aggregate roots.
/// </summary>
public interface IAggregateRoot
{
}

