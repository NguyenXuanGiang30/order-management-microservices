using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.API.Controllers;

/// <summary>
/// Internal API — Chỉ dùng cho giao tiếp service-to-service (không qua Gateway).
/// </summary>
[ApiController]
[Route("internal")]
public class InternalController : ControllerBase
{
    private readonly IProductInventoryDbContext _ctx;
    public InternalController(IProductInventoryDbContext ctx) { _ctx = ctx; }

    /// <summary>GET /internal/products/{id}/price-check — Kiểm tra giá và tồn kho</summary>
    [HttpGet("products/{id:guid}/price-check")]
    public async Task<ActionResult> PriceCheck(Guid id)
    {
        var product = await _ctx.Products.Include(p => p.Unit).Include(p => p.Inventory).AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound(new { message = "Sản phẩm không tồn tại." });

        return Ok(new
        {
            id = product.Id, code = product.Code, name = product.Name,
            unitName = product.Unit?.Name, sellPrice = product.SellPrice, importPrice = product.ImportPrice,
            quantityOnHand = product.Inventory?.QuantityOnHand ?? 0,
            isAvailable = product.IsActive && (product.Inventory?.QuantityOnHand ?? 0) > 0
        });
    }

    /// <summary>POST /internal/inventory/reserve — Giữ hàng cho đơn</summary>
    [HttpPost("inventory/reserve")]
    public async Task<ActionResult> Reserve([FromBody] ReserveRequest request)
    {
        foreach (var item in request.Items)
        {
            var inventory = await _ctx.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
            if (inventory == null || (inventory.QuantityOnHand - inventory.QuantityReserved) < item.Quantity)
                return BadRequest(new { message = $"Sản phẩm {item.ProductId} không đủ tồn kho khả dụng." });
            inventory.QuantityReserved += item.Quantity;
        }
        await _ctx.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã giữ hàng thành công." });
    }

    /// <summary>POST /internal/inventory/deduct — Trừ kho sau thanh toán</summary>
    [HttpPost("inventory/deduct")]
    public async Task<ActionResult> Deduct([FromBody] DeductRequest request)
    {
        foreach (var item in request.Items)
        {
            var inventory = await _ctx.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
            if (inventory == null) return BadRequest(new { message = $"Không tìm thấy tồn kho cho sản phẩm {item.ProductId}." });

            inventory.QuantityOnHand -= item.Quantity;
            inventory.QuantityReserved = Math.Max(0, inventory.QuantityReserved - item.Quantity);
            inventory.LastUpdated = DateTime.UtcNow;

            _ctx.InventoryTransactions.Add(new Application.Models.InventoryTransaction
            {
                ProductId = item.ProductId, TransactionType = "Export", QuantityChange = -item.Quantity,
                QuantityAfter = inventory.QuantityOnHand, ReferenceType = "Order",
                ReferenceId = request.OrderId, Note = $"Trừ kho cho đơn hàng", CreatedBy = Guid.Empty
            });
        }
        await _ctx.SaveChangesAsync();
        return Ok(new { success = true, message = "Trừ kho thành công." });
    }
}

public record ReserveRequest(List<ReserveItem> Items);
public record ReserveItem(Guid ProductId, int Quantity);
public record DeductRequest(Guid OrderId, List<DeductItem> Items);
public record DeductItem(Guid ProductId, int Quantity);
