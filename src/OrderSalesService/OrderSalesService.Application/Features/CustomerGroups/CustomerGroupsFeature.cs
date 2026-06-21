using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.CustomerGroups;

public record GetCustomerGroupsQuery() : IRequest<List<CustomerGroupDto>>;

public class GetCustomerGroupsQueryHandler : IRequestHandler<GetCustomerGroupsQuery, List<CustomerGroupDto>>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;

    public GetCustomerGroupsQueryHandler(IOrderSalesDbContext ctx, IMapper m)
    {
        _context = ctx;
        _mapper = m;
    }

    public async Task<List<CustomerGroupDto>> Handle(GetCustomerGroupsQuery req, CancellationToken ct)
    {
        var items = await _context.CustomerGroups
            .AsNoTracking()
            .ToListAsync(ct);
        return _mapper.Map<List<CustomerGroupDto>>(items);
    }
}

public record CreateCustomerGroupCommand(string Name, decimal DefaultDiscountPercent, string? Note) 
    : IRequest<CustomerGroupDto>;

public class CreateCustomerGroupCommandHandler : IRequestHandler<CreateCustomerGroupCommand, CustomerGroupDto>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;

    public CreateCustomerGroupCommandHandler(IOrderSalesDbContext ctx, IMapper m)
    {
        _context = ctx;
        _mapper = m;
    }

    public async Task<CustomerGroupDto> Handle(CreateCustomerGroupCommand req, CancellationToken ct)
    {
        var entity = new CustomerGroup
        {
            Name = req.Name,
            DefaultDiscountPercent = req.DefaultDiscountPercent,
            Note = req.Note
        };

        _context.CustomerGroups.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.Map<CustomerGroupDto>(entity);
    }
}

public record UpdateCustomerGroupCommand(Guid Id, string Name, decimal DefaultDiscountPercent, string? Note) 
    : IRequest<CustomerGroupDto?>;

public class UpdateCustomerGroupCommandHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto?>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;

    public UpdateCustomerGroupCommandHandler(IOrderSalesDbContext ctx, IMapper m)
    {
        _context = ctx;
        _mapper = m;
    }

    public async Task<CustomerGroupDto?> Handle(UpdateCustomerGroupCommand req, CancellationToken ct)
    {
        var entity = await _context.CustomerGroups
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (entity == null)
            return null;

        entity.Name = req.Name;
        entity.DefaultDiscountPercent = req.DefaultDiscountPercent;
        entity.Note = req.Note;

        await _context.SaveChangesAsync(ct);

        return _mapper.Map<CustomerGroupDto>(entity);
    }
}

public record DeleteCustomerGroupCommand(Guid Id) : IRequest<bool>;

public class DeleteCustomerGroupCommandHandler : IRequestHandler<DeleteCustomerGroupCommand, bool>
{
    private readonly IOrderSalesDbContext _context;

    public DeleteCustomerGroupCommandHandler(IOrderSalesDbContext ctx)
    {
        _context = ctx;
    }

    public async Task<bool> Handle(DeleteCustomerGroupCommand req, CancellationToken ct)
    {
        var entity = await _context.CustomerGroups
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (entity == null)
            return false;

        _context.CustomerGroups.Remove(entity);
        await _context.SaveChangesAsync(ct);

        return true;
    }
}
