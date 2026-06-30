using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OrderSalesService.Application.DTOs;

namespace OrderSalesService.Application.Features.Orders;

public static class OrderCsvService
{
    public static string ToOrdersCsv(IEnumerable<OrderDto> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("OrderId,OrderCode,CustomerId,CustomerName,CreatedByName,OrderDate,SubTotal,DiscountAmount,PromotionDiscountAmount,FinalAmount,PaidAmount,DebtAmount,PaymentMethod,Status,CreatedAt");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                row.Id.ToString(),
                row.OrderCode,
                row.CustomerId.ToString(),
                row.CustomerName,
                row.CreatedByName,
                row.OrderDate.ToString("O", CultureInfo.InvariantCulture),
                row.SubTotal.ToString(CultureInfo.InvariantCulture),
                row.DiscountAmount.ToString(CultureInfo.InvariantCulture),
                row.PromotionDiscountAmount.ToString(CultureInfo.InvariantCulture),
                row.FinalAmount.ToString(CultureInfo.InvariantCulture),
                row.PaidAmount.ToString(CultureInfo.InvariantCulture),
                row.DebtAmount.ToString(CultureInfo.InvariantCulture),
                row.PaymentMethod ?? "",
                row.Status,
                row.CreatedAt.ToString("O", CultureInfo.InvariantCulture)
            }.Select(Escape)));
        }
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (value == null) return "";
        if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
