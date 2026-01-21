namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Base class for domain events.
/// Domain events represent something that happened within a domain aggregate.
/// They are raised by aggregates and can be published as integration events.
/// </summary>
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; }

    protected DomainEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }

    protected DomainEvent(DateTime occurredOn)
    {
        OccurredOn = occurredOn;
    }
}

