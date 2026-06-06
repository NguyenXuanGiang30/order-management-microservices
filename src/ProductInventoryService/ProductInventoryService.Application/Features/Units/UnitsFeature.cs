using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Features.Units;

public record GetUnitsQuery() : IRequest<List<UnitDto>>;

public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, List<UnitDto>>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;
    public GetUnitsQueryHandler(IProductInventoryDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<List<UnitDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        var units = await _context.Units.Where(u => u.IsActive).AsNoTracking().OrderBy(u => u.Name).ToListAsync(cancellationToken);
        return _mapper.Map<List<UnitDto>>(units);
    }
}

public record CreateUnitCommand(string Name, string? Abbreviation) : IRequest<UnitDto>;

public class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, UnitDto>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;
    public CreateUnitCommandHandler(IProductInventoryDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<UnitDto> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = new Models.Unit { Name = request.Name, Abbreviation = request.Abbreviation, IsActive = true };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<UnitDto>(unit);
    }
}
