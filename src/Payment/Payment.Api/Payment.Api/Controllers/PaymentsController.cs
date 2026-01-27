using Microsoft.AspNetCore.Mvc;
using Payment.Api.DTOs;
using Payment.Api.Mappings;
using Payment.Application.Commands;
using Payment.Application.Handlers;
using Payment.Application.Ports;

namespace Payment.Api.Controllers;

/// <summary>
/// Controller for managing payments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ProcessPaymentCommandHandler _processPaymentHandler;
    private readonly RefundPaymentCommandHandler _refundPaymentHandler;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ProcessPaymentCommandHandler processPaymentHandler,
        RefundPaymentCommandHandler refundPaymentHandler,
        IPaymentRepository paymentRepository,
        ILogger<PaymentsController> logger)
    {
        _processPaymentHandler = processPaymentHandler ?? throw new ArgumentNullException(nameof(processPaymentHandler));
        _refundPaymentHandler = refundPaymentHandler ?? throw new ArgumentNullException(nameof(refundPaymentHandler));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a payment for an order.
    /// </summary>
    /// <param name="request">The payment processing request</param>
    /// <returns>The created payment ID</returns>
    /// <response code="201">Payment processed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment for OrderId {OrderId}, CustomerId {CustomerId}, Amount {Amount}",
                request.OrderId,
                request.CustomerId,
                request.Amount);

            var command = new ProcessPaymentCommand(request.OrderId, request.CustomerId, request.Amount);
            var paymentId = await _processPaymentHandler.HandleAsync(command);

            _logger.LogInformation("Payment {PaymentId} processed successfully", paymentId);

            return CreatedAtAction(
                nameof(GetPaymentById),
                new { id = paymentId },
                new { PaymentId = paymentId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for payment processing");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets a payment by its ID.
    /// </summary>
    /// <param name="id">The payment ID</param>
    /// <returns>The payment details</returns>
    /// <response code="200">Payment found</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting payment {PaymentId}", id);

            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", id);
                return NotFound(new { Error = $"Payment with ID {id} not found" });
            }

            return Ok(payment.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", id);
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets a payment by order ID.
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <returns>The payment details</returns>
    /// <response code="200">Payment found</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentByOrderId([FromQuery] Guid orderId)
    {
        try
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(new { Error = "OrderId is required" });
            }

            _logger.LogInformation("Getting payment for OrderId {OrderId}", orderId);

            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

            if (payment == null)
            {
                _logger.LogWarning("Payment for OrderId {OrderId} not found", orderId);
                return NotFound(new { Error = $"Payment for OrderId {orderId} not found" });
            }

            return Ok(payment.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for OrderId {OrderId}", orderId);
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Refunds a payment.
    /// </summary>
    /// <param name="id">The payment ID</param>
    /// <param name="request">The refund request (optional, can use payment ID from route)</param>
    /// <returns>No content</returns>
    /// <response code="204">Payment refunded successfully</response>
    /// <response code="400">Invalid operation (e.g., payment not succeeded)</response>
    /// <response code="404">Payment not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest? request = null)
    {
        try
        {
            // Use payment ID from route if request body is not provided
            var paymentId = (request != null && request.PaymentId != Guid.Empty) ? request.PaymentId : id;

            _logger.LogInformation("Refunding payment {PaymentId}", paymentId);

            var command = new RefundPaymentCommand(paymentId);
            await _refundPaymentHandler.HandleAsync(command);

            _logger.LogInformation("Payment {PaymentId} refunded successfully", paymentId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Payment not found for refund");
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot refund payment: {Reason}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment");
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
