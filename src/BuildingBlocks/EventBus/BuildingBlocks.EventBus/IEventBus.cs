using BuildingBlocks.Messaging;

namespace BuildingBlocks.EventBus;

/// <summary>
/// Abstraction for event bus that enables publish-subscribe communication
/// between modules (future microservices).
/// 
/// This interface allows modules to communicate via events without direct dependencies.
/// The implementation can be in-memory (for modular monolith) or a message broker
/// like RabbitMQ (for microservices).
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">Type of the integration event</typeparam>
    /// <param name="integrationEvent">The event to publish</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<T>(T integrationEvent) where T : IIntegrationEvent;

    /// <summary>
    /// Subscribes a handler to receive events of the specified type.
    /// </summary>
    /// <typeparam name="T">Type of the integration event</typeparam>
    /// <param name="handler">The handler function to invoke when events are received</param>
    void Subscribe<T>(Func<T, Task> handler) where T : IIntegrationEvent;
}

