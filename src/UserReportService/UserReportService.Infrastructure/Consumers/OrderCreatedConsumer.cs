using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Events;
using UserReportService.Application.Features.Reports;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Infrastructure.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IUserReportDbContext _context;

    public OrderCreatedConsumer(IUserReportDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId?.ToString() ?? $"OrderCreated:{message.OrderId}";

        var isProcessed = await _context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(OrderCreatedConsumer), context.CancellationToken);

        if (isProcessed)
        {
            return;
        }

        var orderDate = message.OrderDate.ToUniversalTime();
        var reportDate = orderDate.Date;
        var year = orderDate.Year;
        var month = orderDate.Month;
        var totalItemsCount = message.Items.Sum(i => i.Quantity);
        var totalCost = message.Items.Sum(i => ResolveItemCost(i));

        await ApplyDailySalesAsync(message, reportDate, totalItemsCount, context.CancellationToken);
        await ApplyMonthlySalesAsync(message, year, month, totalItemsCount, context.CancellationToken);
        await ApplyProfitAsync(message, reportDate, year, month, totalCost, context.CancellationToken);
        await ApplyProductSummariesAsync(message, year, month, context.CancellationToken);
        await ApplyCustomerSummaryAsync(message, year, month, context.CancellationToken);

        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = nameof(OrderCreatedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ApplyDailySalesAsync(OrderCreatedEvent message, DateTime reportDate, int totalItemsCount, CancellationToken ct)
    {
        var dailySummary = await _context.DailySalesSummaries.FirstOrDefaultAsync(d => d.ReportDate == reportDate, ct);
        if (dailySummary == null)
        {
            dailySummary = new DailySalesSummary { ReportDate = reportDate };
            _context.DailySalesSummaries.Add(dailySummary);
        }

        dailySummary.TotalOrders += 1;
        dailySummary.TotalRevenue += message.FinalAmount;
        dailySummary.TotalItemsSold += totalItemsCount;
        dailySummary.LastUpdated = DateTime.UtcNow;
    }

    private async Task ApplyMonthlySalesAsync(OrderCreatedEvent message, int year, int month, int totalItemsCount, CancellationToken ct)
    {
        var monthlySummary = await _context.MonthlySalesSummaries.FirstOrDefaultAsync(m => m.Year == year && m.Month == month, ct);
        if (monthlySummary == null)
        {
            monthlySummary = new MonthlySalesSummary { Year = year, Month = month };
            _context.MonthlySalesSummaries.Add(monthlySummary);
        }

        monthlySummary.TotalOrders += 1;
        monthlySummary.TotalRevenue += message.FinalAmount;
        monthlySummary.TotalItemsSold += totalItemsCount;
        monthlySummary.LastUpdated = DateTime.UtcNow;
    }

    private async Task ApplyProfitAsync(OrderCreatedEvent message, DateTime reportDate, int year, int month, decimal totalCost, CancellationToken ct)
    {
        var dailyProfit = await _context.DailyProfitSummaries.FirstOrDefaultAsync(p => p.ReportDate == reportDate, ct);
        if (dailyProfit == null)
        {
            dailyProfit = new DailyProfitSummary { ReportDate = reportDate };
            _context.DailyProfitSummaries.Add(dailyProfit);
        }

        dailyProfit.TotalOrders += 1;
        dailyProfit.TotalRevenue += message.FinalAmount;
        dailyProfit.TotalCost += totalCost;
        ApplyProfitTotals(dailyProfit);

        var monthlyProfit = await _context.MonthlyProfitSummaries.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);
        if (monthlyProfit == null)
        {
            monthlyProfit = new MonthlyProfitSummary { Year = year, Month = month };
            _context.MonthlyProfitSummaries.Add(monthlyProfit);
        }

        monthlyProfit.TotalOrders += 1;
        monthlyProfit.TotalRevenue += message.FinalAmount;
        monthlyProfit.TotalCost += totalCost;
        ApplyProfitTotals(monthlyProfit);

        foreach (var item in message.Items)
        {
            var productProfit = await _context.ProductProfitSummaries
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month && p.ProductId == item.ProductId, ct);

            if (productProfit == null)
            {
                productProfit = new ProductProfitSummary
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Year = year,
                    Month = month
                };
                _context.ProductProfitSummaries.Add(productProfit);
            }

            productProfit.TotalQuantitySold += item.Quantity;
            productProfit.TotalRevenue += item.SubTotal;
            productProfit.TotalCost += ResolveItemCost(item);
            var profit = ProfitPolicy.Calculate(productProfit.TotalRevenue, productProfit.TotalCost);
            productProfit.GrossProfit = profit.GrossProfit;
            productProfit.MarginPercent = profit.MarginPercent;
            productProfit.LastUpdated = DateTime.UtcNow;
        }
    }

    private async Task ApplyProductSummariesAsync(OrderCreatedEvent message, int year, int month, CancellationToken ct)
    {
        foreach (var item in message.Items)
        {
            var productSummary = await _context.TopProductSummaries
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month && p.ProductId == item.ProductId, ct);

            if (productSummary == null)
            {
                productSummary = new TopProductSummary
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Year = year,
                    Month = month
                };
                _context.TopProductSummaries.Add(productSummary);
            }

            productSummary.TotalQuantitySold += item.Quantity;
            productSummary.TotalRevenueGenerated += item.SubTotal;
            productSummary.LastUpdated = DateTime.UtcNow;
        }
    }

    private async Task ApplyCustomerSummaryAsync(OrderCreatedEvent message, int year, int month, CancellationToken ct)
    {
        var customerSummary = await _context.TopCustomerSummaries
            .FirstOrDefaultAsync(c => c.Year == year && c.Month == month && c.CustomerId == message.CustomerId, ct);

        if (customerSummary == null)
        {
            customerSummary = new TopCustomerSummary
            {
                CustomerId = message.CustomerId,
                CustomerName = message.CustomerName,
                CustomerPhone = "",
                Year = year,
                Month = month
            };
            _context.TopCustomerSummaries.Add(customerSummary);
        }

        customerSummary.TotalOrders += 1;
        customerSummary.TotalSpent += message.FinalAmount;
        customerSummary.LastUpdated = DateTime.UtcNow;
    }

    private static void ApplyProfitTotals(DailyProfitSummary summary)
    {
        var profit = ProfitPolicy.Calculate(summary.TotalRevenue, summary.TotalCost);
        summary.GrossProfit = profit.GrossProfit;
        summary.MarginPercent = profit.MarginPercent;
        summary.LastUpdated = DateTime.UtcNow;
    }

    private static void ApplyProfitTotals(MonthlyProfitSummary summary)
    {
        var profit = ProfitPolicy.Calculate(summary.TotalRevenue, summary.TotalCost);
        summary.GrossProfit = profit.GrossProfit;
        summary.MarginPercent = profit.MarginPercent;
        summary.LastUpdated = DateTime.UtcNow;
    }

    private static decimal ResolveItemCost(OrderCreatedItem item)
    {
        return item.CostTotal > 0 ? item.CostTotal : item.CostPrice * item.Quantity;
    }
}
