using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Customers;

public record GetCustomersQuery(string? Search, int PageNumber = 1, int PageSize = 10)
    : IRequest<PagedResponse<CustomerDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResponse<CustomerDto>>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;
    public GetCustomersQueryHandler(IOrderSalesDbContext ctx, IMapper m) { _context = ctx; _mapper = m; }

    public async Task<PagedResponse<CustomerDto>> Handle(GetCustomersQuery req, CancellationToken ct)
    {
        var q = _context.Customers.Include(c => c.CustomerGroup).Where(c => c.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(c => c.FullName.ToLower().Contains(s) || c.Code.ToLower().Contains(s) || (c.Phone != null && c.Phone.Contains(s)));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.FullName).Skip((req.PageNumber - 1) * req.PageSize).Take(req.PageSize).AsNoTracking().ToListAsync(ct);
        return new PagedResponse<CustomerDto> { Items = _mapper.Map<List<CustomerDto>>(items), PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }
}

public record CreateCustomerCommand(string Code, string FullName, string? Phone, string? Email,
    string? Address, string? TaxCode, Guid? CustomerGroupId, string? Note) : IRequest<CustomerDto>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;
    public CreateCustomerCommandHandler(IOrderSalesDbContext ctx, IMapper m) { _context = ctx; _mapper = m; }

    public async Task<CustomerDto> Handle(CreateCustomerCommand req, CancellationToken ct)
    {
        var customer = new Customer
        {
            Code = req.Code, FullName = req.FullName, Phone = req.Phone, Email = req.Email,
            Address = req.Address, TaxCode = req.TaxCode, CustomerGroupId = req.CustomerGroupId, Note = req.Note
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);

        var created = await _context.Customers.Include(c => c.CustomerGroup)
            .FirstOrDefaultAsync(c => c.Id == customer.Id, ct);
        return _mapper.Map<CustomerDto>(created!);
    }
}
