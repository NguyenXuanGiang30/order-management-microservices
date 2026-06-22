using System;
using System.Collections.Generic;
using System.Linq;
using UserReportService.Application.Models;

namespace UserReportService.Infrastructure.Data;

public static class DbInitializer
{
    public static void SeedData(UserReportDbContext context)
    {
        if (context.DailySalesSummaries.Any())
        {
            return;
        }

        var random = new Random(42);

        var sampleProducts = new[]
        {
            new { Id = Guid.Parse("f0010001-1111-2222-3333-444455556666"), Code = "TP-MHH", Name = "Mì ăn liền Hảo Hảo tôm chua cay 75g", Unit = "Gói", UnitPrice = 4000m, CostPrice = 3200m },
            new { Id = Guid.Parse("f0010002-1111-2222-3333-444455556666"), Code = "TP-DAS", Name = "Dầu ăn Simply nguyên chất 1L", Unit = "Chai", UnitPrice = 52000m, CostPrice = 42000m },
            new { Id = Guid.Parse("f0020001-1111-2222-3333-444455556666"), Code = "NG-COCA", Name = "Nước ngọt Coca Cola lon 320ml", Unit = "Lon", UnitPrice = 10000m, CostPrice = 8000m },
            new { Id = Guid.Parse("f0020002-1111-2222-3333-444455556666"), Code = "NG-KEN", Name = "Bia Heineken Silver lon 330ml", Unit = "Lon", UnitPrice = 20000m, CostPrice = 16000m },
            new { Id = Guid.Parse("f0030001-1111-2222-3333-444455556666"), Code = "SU-VNM", Name = "Sữa tươi tiệt trùng Vinamilk ít đường 180ml", Unit = "Hộp", UnitPrice = 8500m, CostPrice = 6500m },
            new { Id = Guid.Parse("f0040003-1111-2222-3333-444455556666"), Code = "BK-CHOCO", Name = "Bánh ChocoPie Orion hộp 12 cái 396g", Unit = "Hộp", UnitPrice = 55000m, CostPrice = 44000m },
            new { Id = Guid.Parse("f0050002-1111-2222-3333-444455556666"), Code = "HM-OMO", Name = "Bột giặt OMO Comfort tinh dầu thơm túi 3.6kg", Unit = "Túi", UnitPrice = 198000m, CostPrice = 165000m }
        };

        var sampleCustomers = new[]
        {
            new { Id = Guid.Parse("d0000001-1111-2222-3333-444455556666"), FullName = "Nguyễn Văn Anh", Phone = "0912345678", IsVip = false },
            new { Id = Guid.Parse("d0000002-1111-2222-3333-444455556666"), FullName = "Trần Thị Bình", Phone = "0987654321", IsVip = true },
            new { Id = Guid.Parse("d0000003-1111-2222-3333-444455556666"), FullName = "Lê Hoàng Cường", Phone = "0905123456", IsVip = false },
            new { Id = Guid.Parse("d0000004-1111-2222-3333-444455556666"), FullName = "Phạm Minh Đức", Phone = "0936789012", IsVip = false },
            new { Id = Guid.Parse("d0000005-1111-2222-3333-444455556666"), FullName = "Đỗ Thu Hà", Phone = "0977111222", IsVip = true }
        };

        // Class to store raw generated order info for aggregation
        var tempOrders = new List<dynamic>();

        for (int dayOffset = 10; dayOffset >= 0; dayOffset--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-dayOffset).AddHours(8);
            var ordersCount = dayOffset == 0 ? 5 : random.Next(3, 8);

            for (int i = 0; i < ordersCount; i++)
            {
                var customer = sampleCustomers[random.Next(sampleCustomers.Length)];
                var orderDate = date.AddHours(random.Next(0, 10)).AddMinutes(random.Next(0, 60));

                var itemsCount = random.Next(1, 5);
                var chosenProducts = sampleProducts.OrderBy(x => random.Next()).Take(itemsCount).ToList();
                var subTotal = 0m;
                var costTotal = 0m;
                var totalQuantity = 0;

                var items = new List<dynamic>();

                foreach (var prod in chosenProducts)
                {
                    var qty = random.Next(1, 6);
                    var detailSubTotal = qty * prod.UnitPrice;
                    var detailCostTotal = qty * prod.CostPrice;
                    subTotal += detailSubTotal;
                    costTotal += detailCostTotal;
                    totalQuantity += qty;

                    items.Add(new {
                        ProductId = prod.Id,
                        ProductCode = prod.Code,
                        ProductName = prod.Name,
                        Quantity = qty,
                        UnitPrice = prod.UnitPrice,
                        CostPrice = prod.CostPrice,
                        SubTotal = detailSubTotal,
                        CostTotal = detailCostTotal
                    });
                }

                var discountPercent = customer.IsVip ? 5.0m : 0.0m;
                var discountAmount = subTotal * (discountPercent / 100m);
                var finalAmount = subTotal - discountAmount;

                tempOrders.Add(new {
                    Date = orderDate.Date,
                    DateTime = orderDate,
                    CustomerId = customer.Id,
                    CustomerName = customer.FullName,
                    CustomerPhone = customer.Phone,
                    SubTotal = subTotal,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    CostTotal = costTotal,
                    TotalQuantity = totalQuantity,
                    Items = items
                });
            }
        }

        // Aggregate to DailySalesSummary & DailyProfitSummary
        var dailySales = tempOrders.GroupBy(o => o.Date).Select(g => new DailySalesSummary
        {
            Id = Guid.NewGuid(),
            ReportDate = g.Key,
            TotalOrders = g.Count(),
            TotalRevenue = g.Sum(o => (decimal)o.FinalAmount),
            TotalDiscount = g.Sum(o => (decimal)o.DiscountAmount),
            TotalItemsSold = g.Sum(o => (int)o.TotalQuantity),
            LastUpdated = DateTime.UtcNow
        }).ToList();

        var dailyProfits = tempOrders.GroupBy(o => o.Date).Select(g => {
            var revenue = g.Sum(o => (decimal)o.FinalAmount);
            var cost = g.Sum(o => (decimal)o.CostTotal);
            var profit = revenue - cost;
            var margin = revenue > 0 ? (profit / revenue) * 100 : 0;
            return new DailyProfitSummary
            {
                Id = Guid.NewGuid(),
                ReportDate = g.Key,
                TotalRevenue = revenue,
                TotalCost = cost,
                GrossProfit = profit,
                MarginPercent = margin,
                TotalOrders = g.Count(),
                LastUpdated = DateTime.UtcNow
            };
        }).ToList();

        // Aggregate to MonthlySalesSummary & MonthlyProfitSummary
        var monthlySales = tempOrders.GroupBy(o => new { o.Date.Year, o.Date.Month }).Select(g => new MonthlySalesSummary
        {
            Id = Guid.NewGuid(),
            Year = g.Key.Year,
            Month = g.Key.Month,
            TotalOrders = g.Count(),
            TotalRevenue = g.Sum(o => (decimal)o.FinalAmount),
            TotalDiscount = g.Sum(o => (decimal)o.DiscountAmount),
            TotalItemsSold = g.Sum(o => (int)o.TotalQuantity),
            TotalNewCustomers = 5,
            LastUpdated = DateTime.UtcNow
        }).ToList();

        var monthlyProfits = tempOrders.GroupBy(o => new { o.Date.Year, o.Date.Month }).Select(g => {
            var revenue = g.Sum(o => (decimal)o.FinalAmount);
            var cost = g.Sum(o => (decimal)o.CostTotal);
            var profit = revenue - cost;
            var margin = revenue > 0 ? (profit / revenue) * 100 : 0;
            return new MonthlyProfitSummary
            {
                Id = Guid.NewGuid(),
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalRevenue = revenue,
                TotalCost = cost,
                GrossProfit = profit,
                MarginPercent = margin,
                TotalOrders = g.Count(),
                LastUpdated = DateTime.UtcNow
            };
        }).ToList();

        // Aggregate to ProductProfitSummary & TopProductSummary
        var productProfits = tempOrders
            .SelectMany(o => ((IEnumerable<dynamic>)o.Items).Select(item => new { o.Date.Year, o.Date.Month, Item = item }))
            .GroupBy(x => new { x.Year, x.Month, x.Item.ProductId, x.Item.ProductCode, x.Item.ProductName })
            .Select(g => {
                var revenue = g.Sum(x => (decimal)x.Item.SubTotal);
                var cost = g.Sum(x => (decimal)x.Item.CostTotal);
                var profit = revenue - cost;
                var margin = revenue > 0 ? (profit / revenue) * 100 : 0;
                return new ProductProfitSummary
                {
                    Id = Guid.NewGuid(),
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalQuantitySold = g.Sum(x => (int)x.Item.Quantity),
                    TotalRevenue = revenue,
                    TotalCost = cost,
                    GrossProfit = profit,
                    MarginPercent = margin,
                    LastUpdated = DateTime.UtcNow
                };
            }).ToList();

        var topProducts = tempOrders
            .SelectMany(o => ((IEnumerable<dynamic>)o.Items).Select(item => new { o.Date.Year, o.Date.Month, Item = item }))
            .GroupBy(x => new { x.Year, x.Month, x.Item.ProductId, x.Item.ProductCode, x.Item.ProductName })
            .Select(g => new TopProductSummary
            {
                Id = Guid.NewGuid(),
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(x => (int)x.Item.Quantity),
                TotalRevenueGenerated = g.Sum(x => (decimal)x.Item.SubTotal),
                Year = g.Key.Year,
                Month = g.Key.Month,
                LastUpdated = DateTime.UtcNow
            }).ToList();

        // Aggregate to TopCustomerSummary
        var topCustomers = tempOrders
            .GroupBy(o => new { o.Date.Year, o.Date.Month, o.CustomerId, o.CustomerName, o.CustomerPhone })
            .Select(g => new TopCustomerSummary
            {
                Id = Guid.NewGuid(),
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                CustomerPhone = g.Key.CustomerPhone,
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => (decimal)o.FinalAmount),
                Year = g.Key.Year,
                Month = g.Key.Month,
                LastUpdated = DateTime.UtcNow
            }).ToList();

        context.DailySalesSummaries.AddRange(dailySales);
        context.DailyProfitSummaries.AddRange(dailyProfits);
        context.MonthlySalesSummaries.AddRange(monthlySales);
        context.MonthlyProfitSummaries.AddRange(monthlyProfits);
        context.ProductProfitSummaries.AddRange(productProfits);
        context.TopProductSummaries.AddRange(topProducts);
        context.TopCustomerSummaries.AddRange(topCustomers);
        context.SaveChanges();
    }
}
