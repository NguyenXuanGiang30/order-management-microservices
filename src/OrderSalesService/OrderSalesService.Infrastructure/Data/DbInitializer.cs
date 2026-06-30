using System;
using System.Collections.Generic;
using System.Linq;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Infrastructure.Data;

public static class DbInitializer
{
    public static void SeedData(OrderSalesDbContext context)
    {
        // Đảm bảo luôn có Khách vãng lai
        var guestCustomer = context.Customers.FirstOrDefault(c => c.Code == "KHL");
        if (guestCustomer == null)
        {
            guestCustomer = new Customer
            {
                Id = Guid.Parse("d0000000-1111-2222-3333-444455556666"),
                Code = "KHL",
                FullName = "Khách vãng lai",
                Phone = "0999999999",
                Email = "retail@system.com",
                Address = "Tại quầy",
                CustomerGroupId = Guid.Parse("c24f74d1-55b2-4d2a-8742-5f657a8a25c1"), // Khách Lẻ
                TotalPurchased = 0m,
                DebtAmount = 0m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Customers.Add(guestCustomer);
            context.SaveChanges();
        }

        if (context.Orders.Any())
        {
            return;
        }

        var adminUserId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
        var customers = context.Customers.ToList();
        if (!customers.Any()) return;

        var random = new Random(42); // Seeded for reproducibility
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

        var orders = new List<Order>();
        var orderIndex = 1;

        // Generate orders for the last 10 days
        for (int dayOffset = 10; dayOffset >= 0; dayOffset--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-dayOffset).AddHours(8); // Starting at 8 AM
            var ordersCount = dayOffset == 0 ? 5 : random.Next(3, 8); // 5 orders for today, random for others

            for (int i = 0; i < ordersCount; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var orderDate = date.AddHours(random.Next(0, 10)).AddMinutes(random.Next(0, 60));
                
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderCode = $"HD{orderDate:yyyyMMdd}{orderIndex++:D4}",
                    CustomerId = customer.Id,
                    CustomerName = customer.FullName,
                    CreatedBy = adminUserId,
                    CreatedByName = "System Administrator",
                    PaymentMethod = random.Next(2) == 0 ? "Cash" : "BankTransfer",
                    Status = "Completed",
                    Note = $"Đơn hàng bán lẻ ngày {orderDate:dd/MM/yyyy}",
                    CreatedAt = orderDate,
                    UpdatedAt = orderDate
                };

                // Add 1 to 4 items
                var itemsCount = random.Next(1, 5);
                var chosenProducts = sampleProducts.OrderBy(x => random.Next()).Take(itemsCount).ToList();
                var subTotal = 0m;

                foreach (var prod in chosenProducts)
                {
                    var qty = random.Next(1, 6);
                    var detailSubTotal = qty * prod.UnitPrice;
                    subTotal += detailSubTotal;

                    order.OrderDetails.Add(new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = prod.Id,
                        ProductCode = prod.Code,
                        ProductName = prod.Name,
                        UnitName = prod.Unit,
                        UnitPrice = prod.UnitPrice,
                        CostPrice = prod.CostPrice,
                        CostTotal = qty * prod.CostPrice,
                        Quantity = qty,
                        DiscountPercent = 0,
                        SubTotal = detailSubTotal
                    });
                }

                order.SubTotal = subTotal;
                // Calculate discount: VIP get 5% discount
                var discountPercent = customer.CustomerGroupId == Guid.Parse("e5f02c6b-67a8-44a6-b51f-4b07fb7b4de1") ? 5.0m : 0.0m;
                order.DiscountPercent = discountPercent;
                order.DiscountAmount = subTotal * (discountPercent / 100m);
                order.FinalAmount = subTotal - order.DiscountAmount;
                order.PaidAmount = order.FinalAmount; // fully paid
                order.DebtAmount = 0m;

                orders.Add(order);
            }
        }

        context.Orders.AddRange(orders);
        context.SaveChanges();
    }
}
