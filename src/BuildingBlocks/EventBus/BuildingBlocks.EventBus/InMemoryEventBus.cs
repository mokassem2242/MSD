using BuildingBlocks.Messaging;

namespace BuildingBlocks.EventBus;

/// <summary>
/// In-memory implementation of IEventBus for use in a modular monolith.
///
/// This implementation stores handlers in memory and invokes them synchronously
/// when events are published. Perfect for learning and testing before moving to
/// a distributed message broker like RabbitMQ.
///
/// Note: In a production microservices architecture, this would be replaced
/// with a RabbitMQEventBus or similar implementation.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<IIntegrationEvent, Task>>> _handlers;
    private readonly object _lock = new();

    public InMemoryEventBus()
    {
        _handlers = new Dictionary<Type, List<Func<IIntegrationEvent, Task>>>();
    }

    public Task PublishAsync<T>(T integrationEvent) where T : IIntegrationEvent
    {
        // CRITICAL FIX: Use the actual runtime type of the event object, not the generic type parameter
        // When called as PublishAsync<IIntegrationEvent>(orderCreated), typeof(T) is IIntegrationEvent
        // but integrationEvent.GetType() is OrderCreated, which is what handlers are registered for
        var eventType = integrationEvent.GetType();

        List<Func<IIntegrationEvent, Task>>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
            {
                // No handlers subscribed for this event type
                return Task.CompletedTask;
            }

            // Create a copy to avoid locking issues during iteration
            handlers = new List<Func<IIntegrationEvent, Task>>(handlers);
        }

        // Invoke all handlers asynchronously
        var tasks = handlers.Select(handler => handler(integrationEvent));
        return Task.WhenAll(tasks);
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IIntegrationEvent
    {
        var eventType = typeof(T);

        // Wrap the typed handler to match the dictionary signature
        Func<IIntegrationEvent, Task> wrappedHandler = async (evt) =>
        {
            if (evt is T typedEvent)
            {
                await handler(typedEvent);
            }
        };

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Func<IIntegrationEvent, Task>>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(wrappedHandler);
        }
    }
}
