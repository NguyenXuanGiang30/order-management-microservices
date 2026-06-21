using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.Features.Inventory;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Warehouse")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    public InventoryController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/inventory/receipts — Danh sách phiếu nhập</summary>
    [HttpGet("receipts")]
    public async Task<ActionResult<ApiResponse<PagedResponse<GoodsReceiptDto>>>> GetReceipts(
        [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetGoodsReceiptsQuery(status, from, to, page, pageSize));
        return Ok(ApiResponse<PagedResponse<GoodsReceiptDto>>.SuccessResponse(result));
    }

    /// <summary>POST /api/inventory/receipts — Tạo phiếu nhập (Draft)</summary>
    [HttpPost("receipts")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> CreateReceipt([FromBody] CreateGoodsReceiptCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetReceipts), ApiResponse<GoodsReceiptDto>.SuccessResponse(result, "Tạo phiếu nhập thành công (Draft)."));
    }

    /// <summary>POST /api/inventory/receipts/import-items — Nhập danh sách mặt hàng từ file CSV</summary>
    [HttpPost("receipts/import-items")]
    public async Task<ActionResult<ApiResponse<List<ImportedReceiptItemResultDto>>>> ImportReceiptItems(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<List<ImportedReceiptItemResultDto>>.FailResponse("File CSV trống hoặc không tồn tại."));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync();
        
        try
        {
            var result = await _mediator.Send(new ParseGoodsReceiptCsvQuery(csv));
            return Ok(ApiResponse<List<ImportedReceiptItemResultDto>>.SuccessResponse(result, "Nhập danh sách mặt hàng thành công."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<List<ImportedReceiptItemResultDto>>.FailResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<ImportedReceiptItemResultDto>>.FailResponse($"Đã xảy ra lỗi hệ thống: {ex.Message}"));
        }
    }

    /// <summary>PUT /api/inventory/receipts/{id}/confirm — Xác nhận phiếu nhập</summary>
    [HttpPut("receipts/{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmReceipt(Guid id)
    {
        var result = await _mediator.Send(new ConfirmGoodsReceiptCommand(id, Guid.Empty));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy phiếu nhập hoặc phiếu không ở trạng thái Draft."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xác nhận phiếu nhập thành công. Tồn kho đã được cập nhật."));
    }

    /// <summary>PUT /api/inventory/receipts/{id}/cancel — Hủy phiếu nhập</summary>
    [HttpPut("receipts/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelReceipt(Guid id)
    {
        var result = await _mediator.Send(new CancelGoodsReceiptCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy phiếu nhập hoặc phiếu không ở trạng thái Draft."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Hủy phiếu nhập thành công."));
    }

    /// <summary>GET /api/inventory/stock — Xem tồn kho</summary>
    [HttpGet("stock")]
    public async Task<ActionResult<ApiResponse<List<StockDto>>>> GetStock([FromQuery] bool? belowMin, [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetStockQuery(belowMin, search));
        return Ok(ApiResponse<List<StockDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/inventory/transactions — Lịch sử biến động kho</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResponse<InventoryTransactionDto>>>> GetTransactions(
        [FromQuery] Guid? productId, [FromQuery] string? type, [FromQuery] DateTime? from,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetInventoryTransactionsQuery(productId, type, from, page, pageSize));
        return Ok(ApiResponse<PagedResponse<InventoryTransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("stock/export")]
    public async Task<IActionResult> ExportStock([FromQuery] bool? belowMin, [FromQuery] string? search)
    {
        var stock = await _mediator.Send(new GetStockQuery(belowMin, search));
        var bytes = InventoryCsvService.ToUtf8BomBytes(InventoryCsvService.ToStockCsv(stock));
        return File(bytes, "text/csv; charset=utf-8", $"inventory-stock-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("transactions/export")]
    public async Task<IActionResult> ExportTransactions(
        [FromQuery] Guid? productId, [FromQuery] string? type, [FromQuery] DateTime? from)
    {
        var result = await _mediator.Send(new GetInventoryTransactionsQuery(productId, type, from, 1, 10000));
        var bytes = InventoryCsvService.ToUtf8BomBytes(InventoryCsvService.ToTransactionsCsv(result.Items));
        return File(bytes, "text/csv; charset=utf-8", $"inventory-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("stocktakes")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StocktakeSessionDto>>>> GetStocktakes(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetStocktakesQuery(status, page, pageSize));
        return Ok(ApiResponse<PagedResponse<StocktakeSessionDto>>.SuccessResponse(result));
    }

    [HttpGet("stocktakes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StocktakeSessionDto>>> GetStocktake(Guid id)
    {
        var result = await _mediator.Send(new GetStocktakeByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<StocktakeSessionDto>.FailResponse("Stocktake session not found."));
        return Ok(ApiResponse<StocktakeSessionDto>.SuccessResponse(result));
    }

    [HttpPost("stocktakes")]
    public async Task<ActionResult<ApiResponse<StocktakeSessionDto>>> CreateStocktake([FromBody] CreateStocktakeSessionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetStocktake), new { id = result.Id },
            ApiResponse<StocktakeSessionDto>.SuccessResponse(result, "Stocktake session created."));
    }

    [HttpPut("stocktakes/{id:guid}/lines")]
    public async Task<ActionResult<ApiResponse<StocktakeSessionDto>>> UpdateStocktakeLines(Guid id, [FromBody] List<UpdateStocktakeLineItemDto> lines)
    {
        var result = await _mediator.Send(new UpdateStocktakeLinesCommand(id, lines));
        if (result == null) return NotFound(ApiResponse<StocktakeSessionDto>.FailResponse("Stocktake session not found or not editable."));
        return Ok(ApiResponse<StocktakeSessionDto>.SuccessResponse(result, "Stocktake lines updated."));
    }

    [HttpPut("stocktakes/{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<StocktakeSessionDto>>> ConfirmStocktake(Guid id)
    {
        var result = await _mediator.Send(new ConfirmStocktakeCommand(id, Guid.Empty));
        if (result == null) return NotFound(ApiResponse<StocktakeSessionDto>.FailResponse("Stocktake session not found or not editable."));
        return Ok(ApiResponse<StocktakeSessionDto>.SuccessResponse(result, "Stocktake confirmed and inventory adjusted."));
    }

    [HttpPut("stocktakes/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelStocktake(Guid id)
    {
        var result = await _mediator.Send(new CancelStocktakeCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Stocktake session not found or not editable."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Stocktake session cancelled."));
    }

    [HttpGet("stocktakes/{id:guid}/template")]
    public async Task<IActionResult> ExportStocktakeTemplate(Guid id)
    {
        var result = await _mediator.Send(new GetStocktakeByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<StocktakeSessionDto>.FailResponse("Stocktake session not found."));

        var bytes = InventoryCsvService.ToUtf8BomBytes(InventoryCsvService.ToStocktakeTemplateCsv(result));
        return File(bytes, "text/csv; charset=utf-8", $"stocktake-{result.StocktakeCode}.csv");
    }

    [HttpPost("stocktakes/{id:guid}/import")]
    public async Task<ActionResult<ApiResponse<StocktakeSessionDto>>> ImportStocktakeCounts(Guid id, IFormFile file)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<StocktakeSessionDto>.FailResponse("CSV file is empty."));

        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync();
        var result = await _mediator.Send(new ImportStocktakeCountsCommand(id, csv));
        if (result == null) return NotFound(ApiResponse<StocktakeSessionDto>.FailResponse("Stocktake session not found or not editable."));

        return Ok(ApiResponse<StocktakeSessionDto>.SuccessResponse(result, "Stocktake counts imported."));
    }
}
