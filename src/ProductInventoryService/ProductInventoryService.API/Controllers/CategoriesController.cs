using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Features.Categories;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());
        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), ApiResponse<CategoryDto>.SuccessResponse(result, "Tạo danh mục thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, dto.Name, dto.Description, dto.ParentId, dto.SortOrder));
        if (result == null) return NotFound(ApiResponse<CategoryDto>.FailResponse("Không tìm thấy danh mục."));
        return Ok(ApiResponse<CategoryDto>.SuccessResponse(result, "Cập nhật thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy danh mục."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa danh mục thành công (Soft Delete)."));
    }
}
