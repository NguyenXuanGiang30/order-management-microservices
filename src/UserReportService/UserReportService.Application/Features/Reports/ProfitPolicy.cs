namespace UserReportService.Application.Features.Reports;

public record ProfitResult(decimal Revenue, decimal Cost, decimal GrossProfit, decimal MarginPercent);

public static class ProfitPolicy
{
    public static ProfitResult Calculate(decimal revenue, decimal cost)
    {
        if (revenue < 0) throw new InvalidOperationException("Revenue cannot be negative.");
        if (cost < 0) throw new InvalidOperationException("Cost cannot be negative.");

        var grossProfit = revenue - cost;
        var marginPercent = revenue <= 0 ? 0 : Math.Round(grossProfit / revenue * 100, 2);

        return new ProfitResult(revenue, cost, grossProfit, marginPercent);
    }
}
