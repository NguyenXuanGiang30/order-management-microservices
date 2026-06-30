using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Interfaces;

/// <summary>
/// Interface trừu tượng hóa DbContext cho ProductInventoryDB.
/// Giúp tầng Application tương tác với DB mà không phụ thuộc trực tiếp vào Infrastructure.
/// </summary>
public interface IProductInventoryDbContext
{
    DbSet<Category> Categories { get; set; }
    DbSet<Unit> Units { get; set; }
    DbSet<Product> Products { get; set; }
    DbSet<Inventory> Inventories { get; set; }
    DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }
    DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    DbSet<StocktakeSession> StocktakeSessions { get; set; }
    DbSet<StocktakeLine> StocktakeLines { get; set; }
    DbSet<ProcessedEvent> ProcessedEvents { get; set; }
    DbSet<UnitConversion> UnitConversions { get; set; }
    DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
