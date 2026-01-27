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
        
        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.PublishAsync:ENTRY", message = "Publishing integration event", data = new { eventType = eventType.Name, genericType = typeof(T).Name, eventId = integrationEvent.Id, totalHandlers = _handlers.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.PublishAsync:ENTRY - runtimeType={eventType.Name}, genericType={typeof(T).Name}, handlers={_handlers.Count}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        List<Func<IIntegrationEvent, Task>>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
            {
                // #region agent log
                try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.PublishAsync:NO_HANDLERS", message = "No handlers found for event type", data = new { eventType = eventType.Name, registeredTypes = _handlers.Keys.Select(k => k.Name).ToArray() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.PublishAsync:NO_HANDLERS - {eventType.Name}, registered={string.Join(",", _handlers.Keys.Select(k => k.Name))}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
                // #endregion
                // No handlers subscribed for this event type
                return Task.CompletedTask;
            }
            
            // Create a copy to avoid locking issues during iteration
            handlers = new List<Func<IIntegrationEvent, Task>>(handlers);
        }

        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.PublishAsync:HANDLERS_FOUND", message = "Handlers found for event", data = new { eventType = eventType.Name, handlerCount = handlers.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.PublishAsync:HANDLERS_FOUND - {eventType.Name}, count={handlers.Count}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        // Invoke all handlers asynchronously
        var tasks = handlers.Select(handler => handler(integrationEvent));
        var result = Task.WhenAll(tasks);

        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.PublishAsync:INVOKED", message = "Handlers invoked", data = new { eventType = eventType.Name, handlerCount = handlers.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.PublishAsync:INVOKED - {eventType.Name}, count={handlers.Count}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        return result;
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IIntegrationEvent
    {
        var eventType = typeof(T);
        
        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.Subscribe:ENTRY", message = "Subscribing handler", data = new { eventType = eventType.Name, currentHandlerCount = _handlers.Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.Subscribe:ENTRY - {eventType.Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        // Wrap the typed handler to match the dictionary signature
        Func<IIntegrationEvent, Task> wrappedHandler = async (evt) =>
        {
            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "InMemoryEventBus.wrappedHandler:ENTRY", message = "Wrapped handler invoked", data = new { eventType = eventType.Name, receivedEventType = evt.GetType().Name, isMatch = evt is T }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.wrappedHandler:ENTRY - expected={eventType.Name}, received={evt.GetType().Name}, match={evt is T}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            if (evt is T typedEvent)
            {
                // #region agent log
                try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "InMemoryEventBus.wrappedHandler:BEFORE_CALL", message = "About to call handler", data = new { eventType = eventType.Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.wrappedHandler:BEFORE_CALL - {eventType.Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
                // #endregion

                await handler(typedEvent);

                // #region agent log
                try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "InMemoryEventBus.wrappedHandler:AFTER_CALL", message = "Handler completed", data = new { eventType = eventType.Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.wrappedHandler:AFTER_CALL - {eventType.Name}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
                // #endregion
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

        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "InMemoryEventBus.Subscribe:COMPLETE", message = "Handler subscribed", data = new { eventType = eventType.Name, totalHandlersForType = _handlers[eventType].Count }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] EventBus.Subscribe:COMPLETE - {eventType.Name}, handlers={_handlers[eventType].Count}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion
    }
}

