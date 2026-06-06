using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Features.Categories;

// ======================== Queries ========================
public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;
    public GetCategoriesQueryHandler(IProductInventoryDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
        
        // Chuyển toàn bộ các category sang DTO dạng phẳng trước
        var allDtos = categories.Select(c => new CategoryDto(
            c.Id, c.Name, c.Description, c.ParentId, c.SortOrder, c.IsActive, new List<CategoryDto>()
        )).ToList();

        // Xây dựng cây phân cấp (tree hierarchy)
        var tree = new List<CategoryDto>();
        var lookup = allDtos.ToDictionary(dto => dto.Id);

        foreach (var dto in allDtos)
        {
            if (dto.ParentId == null || !lookup.ContainsKey(dto.ParentId.Value))
            {
                tree.Add(dto);
            }
            else
            {
                lookup[dto.ParentId.Value].Children.Add(dto);
            }
        }

        return tree;
    }
}

// ======================== Commands ========================
public record CreateCategoryCommand(string Name, string? Description, Guid? ParentId, int SortOrder) : IRequest<CategoryDto>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;
    public CreateCategoryCommandHandler(IProductInventoryDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(category);
    }
}

public record UpdateCategoryCommand(Guid Id, string Name, string? Description, Guid? ParentId, int SortOrder) : IRequest<CategoryDto?>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto?>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;
    public UpdateCategoryCommandHandler(IProductInventoryDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<CategoryDto?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null) return null;

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(category);
    }
}

public record DeleteCategoryCommand(Guid Id) : IRequest<bool>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IProductInventoryDbContext _context;
    public DeleteCategoryCommandHandler(IProductInventoryDbContext context) { _context = context; }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null) return false;

        category.IsActive = false; // Soft delete
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
