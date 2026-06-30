using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Interfaces;

public interface IOrderSalesDbContext
{
    DbSet<CustomerGroup> CustomerGroups { get; set; }
    DbSet<Customer> Customers { get; set; }
    DbSet<Supplier> Suppliers { get; set; }
    DbSet<Order> Orders { get; set; }
    DbSet<OrderDetail> OrderDetails { get; set; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    DbSet<ReturnOrder> ReturnOrders { get; set; }
    DbSet<ReturnOrderDetail> ReturnOrderDetails { get; set; }
    DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    DbSet<CashShift> CashShifts { get; set; }
    DbSet<Promotion> Promotions { get; set; }
    DbSet<PromotionItem> PromotionItems { get; set; }
    DbSet<ProcessedEvent> ProcessedEvents { get; set; }
    DbSet<CashTransaction> CashTransactions { get; set; }
    DbSet<SupplierPayment> SupplierPayments { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
