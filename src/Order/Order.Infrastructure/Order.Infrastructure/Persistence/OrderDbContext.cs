using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Order.Infrastructure.DomainEvents;
using OrderAggregate = Order.Domain.Aggregates.Order;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Order module.
/// Manages database connections and entity configurations.
/// Publishes domain events after saving changes.
/// </summary>
public class OrderDbContext : DbContext
{
    private readonly DomainEventDispatcher? _domainEventDispatcher;

    public DbSet<OrderAggregate> Orders { get; set; }

    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public OrderDbContext(
        DbContextOptions<OrderDbContext> options,
        DomainEventDispatcher? domainEventDispatcher = null)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new Configurations.OrderConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Save changes first
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Ignore events if no dispatcher provided
        if (_domainEventDispatcher == null) return result;

        // Collect domain events from all aggregates in the change tracker
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToArray();

        // Dispatch domain events for each aggregate (generic - works with any aggregate)
        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            // Convert domain events to integration events and publish
            // Generic method - works with any AggregateRoot<Guid>
            if (_domainEventDispatcher != null)
            {
                await _domainEventDispatcher.DispatchDomainEventsAsync(events, aggregate);
            }
        }

        return result;
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }
}

