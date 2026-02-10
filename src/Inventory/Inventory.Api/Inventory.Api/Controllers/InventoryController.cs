using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly Inventory.Application.Ports.IInventoryItemRepository _inventoryRepository;

    public InventoryController(Inventory.Application.Ports.IInventoryItemRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
    }

    /// <summary>
    /// Get stock for a product by product ID.
    /// </summary>
    [HttpGet("items/{productId}")]
    [ProducesResponseType(typeof(InventoryItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemResponse>> GetByProductId(string productId)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(productId);
        if (item == null)
            return NotFound();

        return Ok(new InventoryItemResponse
        {
            ProductId = item.ProductId,
            QuantityInStock = item.QuantityInStock,
            QuantityReserved = item.QuantityReserved,
            AvailableQuantity = item.AvailableQuantity
        });
    }

    /// <summary>
    /// Get all inventory items (stock levels).
    /// </summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(IEnumerable<InventoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryItemResponse>>> GetAll()
    {
        var items = await _inventoryRepository.GetAllAsync();
        var response = items.Select(x => new InventoryItemResponse
        {
            ProductId = x.ProductId,
            QuantityInStock = x.QuantityInStock,
            QuantityReserved = x.QuantityReserved,
            AvailableQuantity = x.AvailableQuantity
        });
        return Ok(response);
    }
}

public record InventoryItemResponse
{
    public string ProductId { get; init; } = "";
    public int QuantityInStock { get; init; }
    public int QuantityReserved { get; init; }
    public int AvailableQuantity { get; init; }
}
