using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Features.Products;
using ProductInventoryService.Application.Features.Products.Commands.CreateProduct;
using ProductInventoryService.Application.Features.Products.Queries.GetProductById;
using ProductInventoryService.Application.Features.Products.Queries.GetProducts;
using ProductInventoryService.Application.Features.Products.Queries.GetProductByBarcode;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Infrastructure.Data;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/products — Danh sách sản phẩm (phân trang, lọc, sắp xếp)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> GetProducts(
        [FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name", [FromQuery] bool sortDescending = false)
    {
        var result = await _mediator.Send(new GetProductsQuery(search, categoryId, isActive, pageNumber, pageSize, sortBy, sortDescending));
        return Ok(ApiResponse<PagedResponse<ProductDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/products/{id} — Chi tiết sản phẩm</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<ProductDto>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result));
    }

    /// <summary>GET /api/products/barcode/{barcode} — Chi tiết sản phẩm theo barcode</summary>
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductByBarcode(string barcode)
    {
        var result = await _mediator.Send(new GetProductByBarcodeQuery(barcode));
        if (result == null) return NotFound(ApiResponse<ProductDto>.FailResponse("Không tìm thấy sản phẩm với mã vạch này."));
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result));
    }

    /// <summary>POST /api/products — Tạo sản phẩm mới</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<CreateProductResponse>>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id },
            ApiResponse<CreateProductResponse>.SuccessResponse(result, "Tạo sản phẩm thành công."));
    }

    /// <summary>PUT /api/products/{id} — Cập nhật sản phẩm</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (result == null) return NotFound(ApiResponse<ProductDto>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>PUT /api/products/{id}/toggle-active — Ngừng/Mở bán</summary>
    [HttpPut("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleActive(Guid id)
    {
        var result = await _mediator.Send(new ToggleProductActiveCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật trạng thái thành công."));
    }

    /// <summary>POST /api/products/{id}/image — Tải lên ảnh sản phẩm</summary>
    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<string>.FailResponse("Vui lòng chọn một file ảnh hợp lệ."));
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<string>.FailResponse("Định dạng file không được hỗ trợ. Chỉ nhận file ảnh (.jpg, .png, .webp...)"));
        }

        var dbContext = HttpContext.RequestServices.GetRequiredService<IProductInventoryDbContext>();
        var product = await dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(ApiResponse<string>.FailResponse("Sản phẩm không tồn tại."));
        }

        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{id}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"/uploads/products/{fileName}";
        product.ImageUrl = imageUrl;
        await dbContext.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse(imageUrl, "Tải lên ảnh sản phẩm thành công."));
    }

    /// <summary>GET /api/products/{id}/price-history — Xem lịch sử giá</summary>
    [HttpGet("{id:guid}/price-history")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<ProductPriceHistoryDto>>>> GetPriceHistory(Guid id)
    {
        var result = await _mediator.Send(new GetProductPriceHistoryQuery(id));
        return Ok(ApiResponse<List<ProductPriceHistoryDto>>.SuccessResponse(result, "Lấy lịch sử giá thành công."));
    }

    /// <summary>POST /api/products/database/seed — Reset và nạp lại dữ liệu mẫu</summary>
    [HttpPost("database/seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SeedDatabase()
    {
        var dbContext = HttpContext.RequestServices.GetRequiredService<IProductInventoryDbContext>();
        
        dbContext.StocktakeLines.RemoveRange(dbContext.StocktakeLines);
        dbContext.StocktakeSessions.RemoveRange(dbContext.StocktakeSessions);
        dbContext.ProductPriceHistories.RemoveRange(dbContext.ProductPriceHistories);
        dbContext.UnitConversions.RemoveRange(dbContext.UnitConversions);
        dbContext.GoodsReceiptDetails.RemoveRange(dbContext.GoodsReceiptDetails);
        dbContext.GoodsReceipts.RemoveRange(dbContext.GoodsReceipts);
        dbContext.InventoryTransactions.RemoveRange(dbContext.InventoryTransactions);

        foreach (var inv in dbContext.Inventories)
        {
            inv.QuantityOnHand = 100;
            inv.QuantityReserved = 0;
            inv.LastUpdated = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        if (dbContext is ProductInventoryDbContext concreteContext)
        {
            DbInitializer.SeedData(concreteContext);
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Reset và nạp lại dữ liệu mẫu kho thành công."));
    }
}
