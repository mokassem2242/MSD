using Microsoft.AspNetCore.Mvc;
using Order.Api.DTOs;
using Order.Api.Mappings;
using Order.Application.Commands;
using Order.Application.Handlers;
using Order.Application.Queries;
using Order.Domain.Enums;

namespace Order.Api.Controllers;

/// <summary>
/// Controller for managing orders.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderCommandHandler _createOrderHandler;
    private readonly GetOrderByIdQueryHandler _getOrderByIdHandler;
    private readonly GetOrdersQueryHandler _getOrdersHandler;
    private readonly CancelOrderCommandHandler _cancelOrderHandler;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        CreateOrderCommandHandler createOrderHandler,
        GetOrderByIdQueryHandler getOrderByIdHandler,
        GetOrdersQueryHandler getOrdersHandler,
        CancelOrderCommandHandler cancelOrderHandler,
        ILogger<OrdersController> logger)
    {
        _createOrderHandler = createOrderHandler ?? throw new ArgumentNullException(nameof(createOrderHandler));
        _getOrderByIdHandler = getOrderByIdHandler ?? throw new ArgumentNullException(nameof(getOrderByIdHandler));
        _getOrdersHandler = getOrdersHandler ?? throw new ArgumentNullException(nameof(getOrdersHandler));
        _cancelOrderHandler = cancelOrderHandler ?? throw new ArgumentNullException(nameof(cancelOrderHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="request">The order creation request</param>
    /// <returns>The created order ID</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

            var orderItems = request.Items.Select(item => new OrderItemCommand
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            var command = new CreateOrderCommand(request.CustomerId, orderItems);
            var orderId = await _createOrderHandler.HandleAsync(command);

            _logger.LogInformation("Order {OrderId} created successfully", orderId);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = orderId },
                new { OrderId = orderId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for order creation");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets an order by its ID.
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>The order details</returns>
    /// <response code="200">Order found</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting order {OrderId}", id);

            var query = new GetOrderByIdQuery(id);
            var order = await _getOrderByIdHandler.HandleAsync(query);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return NotFound(new { Error = $"Order with ID {id} not found" });
            }

            return Ok(order.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets orders with optional filtering.
    /// </summary>
    /// <param name="customerId">Filter by customer ID</param>
    /// <param name="status">Filter by order status</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of orders</returns>
    /// <response code="200">Orders retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<OrderResponse>>> GetOrders(
        [FromQuery] string? customerId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null)
    {
        try
        {
            _logger.LogInformation(
                "Getting orders - CustomerId: {CustomerId}, Status: {Status}, Skip: {Skip}, Take: {Take}",
                customerId, status, skip, take);

            var query = new GetOrdersQuery(customerId, status, skip, take);
            var orders = await _getOrdersHandler.HandleAsync(query);

            return Ok(orders.ToResponseList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Cancels an order.
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="request">Optional cancellation reason</param>
    /// <returns>No content</returns>
    /// <response code="204">Order cancelled successfully</response>
    /// <response code="400">Invalid operation (e.g., order already completed)</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Cancelling order {OrderId}", id);

            var command = new CancelOrderCommand(id, request?.Reason);
            await _cancelOrderHandler.HandleAsync(command);

            _logger.LogInformation("Order {OrderId} cancelled successfully", id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for cancellation", id);
            return NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel order {OrderId}: {Reason}", id, ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

