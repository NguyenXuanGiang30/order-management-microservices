using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Suppliers;

public record SupplierDto(Guid Id, string Code, string Name, string? ContactPerson, string? ContactPhone, string? ContactEmail, string? Address, string? TaxCode, decimal DebtAmount, string? Note, DateTime CreatedAt);

public record GetSuppliersQuery(string? Search, int PageNumber = 1, int PageSize = 20) : IRequest<PagedResponse<SupplierDto>>;
public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PagedResponse<SupplierDto>>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetSuppliersQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<PagedResponse<SupplierDto>> Handle(GetSuppliersQuery req, CancellationToken ct)
    {
        var q = _ctx.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s) || x.Code.ToLower().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.Name).Skip((req.PageNumber - 1) * req.PageSize).Take(req.PageSize)
            .Select(x => new SupplierDto(x.Id, x.Code, x.Name, x.ContactPerson, x.ContactPhone, x.ContactEmail, x.Address, x.TaxCode, x.DebtAmount, x.Note, x.CreatedAt)).ToListAsync(ct);
        return new PagedResponse<SupplierDto> { Items = items, PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }
}

public record CreateSupplierCommand(string Code, string Name, string? ContactPerson, string? ContactPhone, string? ContactEmail, string? Address, string? TaxCode, string? Note, Guid CreatedBy) : IRequest<SupplierDto>;
public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly IOrderSalesDbContext _ctx;
    public CreateSupplierCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<SupplierDto> Handle(CreateSupplierCommand req, CancellationToken ct)
    {
        var supplier = new Supplier { Code = req.Code, Name = req.Name, ContactPerson = req.ContactPerson, ContactPhone = req.ContactPhone, ContactEmail = req.ContactEmail, Address = req.Address, TaxCode = req.TaxCode, Note = req.Note, CreatedBy = req.CreatedBy };
        _ctx.Suppliers.Add(supplier);
        await _ctx.SaveChangesAsync(ct);
        return new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.ContactPerson, supplier.ContactPhone, supplier.ContactEmail, supplier.Address, supplier.TaxCode, supplier.DebtAmount, supplier.Note, supplier.CreatedAt);
    }
}

public record UpdateSupplierCommand(Guid Id, string? Name, string? ContactPerson, string? ContactPhone, string? ContactEmail, string? Address, string? TaxCode, string? Note) : IRequest<SupplierDto?>;
public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto?>
{
    private readonly IOrderSalesDbContext _ctx;
    public UpdateSupplierCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<SupplierDto?> Handle(UpdateSupplierCommand req, CancellationToken ct)
    {
        var s = await _ctx.Suppliers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (s == null) return null;
        if (req.Name != null) s.Name = req.Name;
        if (req.ContactPerson != null) s.ContactPerson = req.ContactPerson;
        if (req.ContactPhone != null) s.ContactPhone = req.ContactPhone;
        if (req.ContactEmail != null) s.ContactEmail = req.ContactEmail;
        if (req.Address != null) s.Address = req.Address;
        if (req.TaxCode != null) s.TaxCode = req.TaxCode;
        if (req.Note != null) s.Note = req.Note;
        await _ctx.SaveChangesAsync(ct);
        return new SupplierDto(s.Id, s.Code, s.Name, s.ContactPerson, s.ContactPhone, s.ContactEmail, s.Address, s.TaxCode, s.DebtAmount, s.Note, s.CreatedAt);
    }
}
