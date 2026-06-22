using System;
using System.Collections.Generic;
using System.Linq;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Infrastructure.Data;

public static class DbInitializer
{
    public static void SeedData(ProductInventoryDbContext context)
    {
        if (context.GoodsReceipts.Any())
        {
            return;
        }

        var adminUserId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");

        var products = context.Products.ToList();
        var inventories = context.Inventories.ToList();
        if (!products.Any()) return;

        var sampleSuppliers = new[]
        {
            new { Id = Guid.Parse("e0000001-1111-2222-3333-444455556666"), Name = "Công ty Cổ phần Acecook Việt Nam" },
            new { Id = Guid.Parse("e0000002-1111-2222-3333-444455556666"), Name = "Công ty TNHH Quốc tế Unilever Việt Nam" },
            new { Id = Guid.Parse("e0000003-1111-2222-3333-444455556666"), Name = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)" }
        };

        var receipts = new List<GoodsReceipt>();
        var transactions = new List<InventoryTransaction>();
        var receiptIndex = 1;

        // Create 3 GoodsReceipts over the last 15 days
        var dates = new[] { DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5) };

        for (int i = 0; i < 3; i++)
        {
            var supplier = sampleSuppliers[i % sampleSuppliers.Length];
            var receiptDate = dates[i];
            
            var receipt = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                ReceiptCode = $"PN{receiptDate:yyyyMMdd}{receiptIndex++:D4}",
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                CreatedBy = adminUserId,
                CreatedByName = "System Administrator",
                ReceiptDate = receiptDate,
                Note = $"Nhập kho định kỳ từ {supplier.Name}",
                Status = "Confirmed",
                CreatedAt = receiptDate,
                UpdatedAt = receiptDate
            };

            // Select 2-3 products to import in this receipt
            var chosenProducts = products.Skip(i * 2).Take(3).ToList();
            var totalAmount = 0m;

            foreach (var prod in chosenProducts)
            {
                var qty = 50 + (i * 10); // 50, 60, 70 etc.
                // Find matching cost price or estimate one (usually 80% of selling price if not set, or we can use a lookup)
                var costPrice = prod.SellPrice * 0.8m;
                var subTotal = qty * costPrice;
                totalAmount += subTotal;

                receipt.GoodsReceiptDetails.Add(new GoodsReceiptDetail
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = receipt.Id,
                    ProductId = prod.Id,
                    Quantity = qty,
                    UnitPrice = costPrice,
                    SubTotal = subTotal
                });

                // Update stock in the inventory table
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == prod.Id);
                if (inventory != null)
                {
                    inventory.QuantityOnHand += qty;
                    inventory.LastUpdated = DateTime.UtcNow;
                }

                // Add inventory transaction log
                transactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    ProductId = prod.Id,
                    TransactionType = "Import",
                    QuantityChange = qty,
                    QuantityAfter = (inventory?.QuantityOnHand ?? 100),
                    ReferenceType = "GoodsReceipt",
                    ReferenceId = receipt.Id,
                    Note = "Nhập kho từ phiếu nhập hàng",
                    CreatedBy = adminUserId,
                    CreatedAt = receiptDate
                });
            }

            receipt.TotalAmount = totalAmount;
            receipts.Add(receipt);
        }

        context.GoodsReceipts.AddRange(receipts);
        context.InventoryTransactions.AddRange(transactions);
        context.SaveChanges();
    }
}
