using System.Text.RegularExpressions;

namespace OrderSalesService.Application.Features.Payments.BankTransfers;

public static partial class BankTransferPaymentMatcher
{
    public static string? ExtractOrderCode(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var match = OrderCodeRegex().Match(content);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex(@"DH-\d{6,20}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderCodeRegex();
}
