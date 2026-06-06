using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Suppliers;

public static class SupplierDebtPolicy
{
    public static decimal ApplyConfirmedGoodsReceipt(Supplier supplier, decimal receiptAmount)
    {
        if (receiptAmount <= 0)
        {
            throw new InvalidOperationException("Receipt amount must be greater than zero.");
        }

        supplier.DebtAmount += receiptAmount;
        return supplier.DebtAmount;
    }
}
