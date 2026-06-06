using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Customers;

// ======================== Get Customer By Id (kèm lịch sử mua) ========================
public record CustomerDetailDto(Guid Id, string Code, string FullName, string? Phone, string? Email, string? Address,
    Guid? CustomerGroupId, string? CustomerGroupName, decimal TotalPurchased, decimal DebtAmount, List<RecentOrderDto> RecentOrders);
public record RecentOrderDto(Guid OrderId, string OrderCode, decimal FinalAmount, string Status);

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto?>;
public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto?>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetCustomerByIdQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<CustomerDetailDto?> Handle(GetCustomerByIdQuery req, CancellationToken ct)
    {
        var c = await _ctx.Customers.Include(x => x.CustomerGroup).AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) return null;

        var recentOrders = await _ctx.Orders.AsNoTracking().Where(o => o.CustomerId == req.Id).OrderByDescending(o => o.OrderDate).Take(10)
            .Select(o => new RecentOrderDto(o.Id, o.OrderCode, o.FinalAmount, o.Status)).ToListAsync(ct);

        var totalPurchased = await _ctx.Orders.AsNoTracking().Where(o => o.CustomerId == req.Id && o.Status != "Cancelled").SumAsync(o => o.FinalAmount, ct);

        return new CustomerDetailDto(c.Id, c.Code, c.FullName, c.Phone, c.Email, c.Address,
            c.CustomerGroupId, c.CustomerGroup?.Name, totalPurchased, c.DebtAmount, recentOrders);
    }
}

// ======================== Update Customer ========================
public record UpdateCustomerCommand(Guid Id, string? FullName, string? Phone, string? Email, string? Address, Guid? CustomerGroupId) : IRequest<bool>;
public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, bool>
{
    private readonly IOrderSalesDbContext _ctx;
    public UpdateCustomerCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<bool> Handle(UpdateCustomerCommand req, CancellationToken ct)
    {
        var c = await _ctx.Customers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c == null) return false;
        if (req.FullName != null) c.FullName = req.FullName;
        if (req.Phone != null) c.Phone = req.Phone;
        if (req.Email != null) c.Email = req.Email;
        if (req.Address != null) c.Address = req.Address;
        if (req.CustomerGroupId.HasValue) c.CustomerGroupId = req.CustomerGroupId;
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}

// ======================== Get Customer Debt ========================
public record CustomerDebtDto(Guid CustomerId, string CustomerName, decimal TotalDebt, List<UnpaidOrderDto> UnpaidOrders);
public record UnpaidOrderDto(string OrderCode, decimal FinalAmount, decimal PaidAmount, decimal DebtAmount);

public record GetCustomerDebtQuery(Guid CustomerId) : IRequest<CustomerDebtDto?>;
public class GetCustomerDebtQueryHandler : IRequestHandler<GetCustomerDebtQuery, CustomerDebtDto?>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetCustomerDebtQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<CustomerDebtDto?> Handle(GetCustomerDebtQuery req, CancellationToken ct)
    {
        var c = await _ctx.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.CustomerId, ct);
        if (c == null) return null;

        var unpaid = await _ctx.Orders.AsNoTracking().Where(o => o.CustomerId == req.CustomerId && o.DebtAmount > 0 && o.Status != "Cancelled")
            .Select(o => new UnpaidOrderDto(o.OrderCode, o.FinalAmount, o.PaidAmount, o.DebtAmount)).ToListAsync(ct);

        return new CustomerDebtDto(c.Id, c.FullName, c.DebtAmount, unpaid);
    }
}
