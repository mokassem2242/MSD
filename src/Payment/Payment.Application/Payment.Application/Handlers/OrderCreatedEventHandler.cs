using BuildingBlocks.Messaging;
using Payment.Application.Commands;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Handlers;

/// <summary>
/// Event handler that consumes OrderCreated integration events and triggers payment processing.
/// This handler subscribes to OrderCreated events from the EventBus and processes payments automatically.
/// </summary>
public class OrderCreatedEventHandler
{
    private readonly ProcessPaymentCommandHandler _processPaymentHandler;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        ProcessPaymentCommandHandler processPaymentHandler,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _processPaymentHandler = processPaymentHandler ?? throw new ArgumentNullException(nameof(processPaymentHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the OrderCreated integration event by processing payment for the order.
    /// This method must be idempotent - duplicate events should not create duplicate payments.
    /// </summary>
    public async Task HandleAsync(OrderCreated integrationEvent)
    {
        // #region agent log
        try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "OrderCreatedEventHandler.HandleAsync:ENTRY", message = "Handler invoked", data = new { orderId = integrationEvent.OrderId, customerId = integrationEvent.CustomerId, amount = integrationEvent.TotalAmount }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderCreatedEventHandler:ENTRY - OrderId={integrationEvent.OrderId}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
        // #endregion

        _logger.LogInformation(
            "Received OrderCreated event for OrderId {OrderId}, CustomerId {CustomerId}, Amount {Amount}",
            integrationEvent.OrderId,
            integrationEvent.CustomerId,
            integrationEvent.TotalAmount);

        try
        {
            // Create command from integration event
            var command = new ProcessPaymentCommand(
                integrationEvent.OrderId,
                integrationEvent.CustomerId,
                integrationEvent.TotalAmount);

            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "OrderCreatedEventHandler.HandleAsync:BEFORE_PROCESS", message = "About to process payment", data = new { orderId = command.OrderId, customerId = command.CustomerId, amount = command.Amount }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderCreatedEventHandler:BEFORE_PROCESS - OrderId={command.OrderId}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            // Process payment (handler checks for idempotency internally)
            var paymentId = await _processPaymentHandler.HandleAsync(command);

            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "OrderCreatedEventHandler.HandleAsync:AFTER_PROCESS", message = "Payment processed", data = new { orderId = integrationEvent.OrderId, paymentId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderCreatedEventHandler:AFTER_PROCESS - OrderId={integrationEvent.OrderId}, PaymentId={paymentId}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion

            _logger.LogInformation(
                "Payment processing initiated for OrderId {OrderId}. PaymentId: {PaymentId}",
                integrationEvent.OrderId,
                paymentId);
        }
        catch (Exception ex)
        {
            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "E", location = "OrderCreatedEventHandler.HandleAsync:EXCEPTION", message = "Exception in handler", data = new { orderId = integrationEvent.OrderId, exceptionType = ex.GetType().Name, exceptionMessage = ex.Message, stackTrace = ex.StackTrace }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] OrderCreatedEventHandler:EXCEPTION - {ex.GetType().Name}: {ex.Message}"); } catch { }
            // #endregion

            _logger.LogError(
                ex,
                "Error processing payment for OrderId {OrderId}",
                integrationEvent.OrderId);
            // In a production system, you might want to publish a PaymentFailed event here
            // or implement retry logic. For now, we log and continue.
        }
    }
}
