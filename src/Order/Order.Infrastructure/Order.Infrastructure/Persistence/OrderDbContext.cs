using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Aggregates.Order.Infrastructure.DomainEvents;
using OrderAggregate = Order.Domain.Aggregates.Order.Domain.Aggregates.Order;

namespace Order.Domain.Aggregates.Order.Infrastructure.Persistence;

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
        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:ENTRY", message = "SaveChangesAsync called", data = new { dispatcherNull = _domainEventDispatcher == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderDbContext.SaveChangesAsync:ENTRY - dispatcherNull={_domainEventDispatcher == null}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        // Save changes first
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:AFTER_SAVE", message = "Changes saved", data = new { result, dispatcherNull = _domainEventDispatcher == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderDbContext.SaveChangesAsync:AFTER_SAVE - result={result}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        // Ignore events if no dispatcher provided
        if (_domainEventDispatcher == null)
        {
            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:NO_DISPATCHER", message = "DomainEventDispatcher is null - events will not be published", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine("[DEBUG] OrderDbContext.SaveChangesAsync:NO_DISPATCHER - DomainEventDispatcher is NULL!"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion
            return result;
        }

        // Collect domain events from all aggregates in the change tracker
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToArray();

        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:COLLECTED", message = "Domain events collected", data = new { aggregateCount = aggregatesWithEvents.Length, eventCounts = aggregatesWithEvents.Select(a => new { aggregateId = a.Id, eventCount = a.DomainEvents.Count }).ToArray() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderDbContext.SaveChangesAsync:COLLECTED - {aggregatesWithEvents.Length} aggregates with events"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        // Dispatch domain events for each aggregate (generic - works with any aggregate)
        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:BEFORE_DISPATCH", message = "About to dispatch domain events", data = new { aggregateId = aggregate.Id, eventTypes = events.Select(e => e.GetType().Name).ToArray() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderDbContext.SaveChangesAsync:BEFORE_DISPATCH - aggregateId={aggregate.Id}, events={string.Join(",", events.Select(e => e.GetType().Name))}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            // Convert domain events to integration events and publish
            // Generic method - works with any AggregateRoot<Guid>
            if (_domainEventDispatcher != null)
            {
                await _domainEventDispatcher.DispatchDomainEventsAsync(events, aggregate);
            }

            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "OrderDbContext.SaveChangesAsync:AFTER_DISPATCH", message = "Domain events dispatched", data = new { aggregateId = aggregate.Id }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderDbContext.SaveChangesAsync:AFTER_DISPATCH - aggregateId={aggregate.Id}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion
        }

        return result;
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }
}

