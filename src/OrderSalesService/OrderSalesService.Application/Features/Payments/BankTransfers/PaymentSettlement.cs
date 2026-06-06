using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Payments.BankTransfers;

public static class PaymentSettlement
{
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static PaymentTransaction ApplyBankTransfer(
        Order order,
        Customer? customer,
        decimal amount,
        string externalTransactionId,
        DateTime receivedAt)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        var oldStatus = order.Status;

        var payment = new PaymentTransaction
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = amount,
            PaymentMethod = "Chuyển khoản",
            Note = $"Tự động khớp giao dịch ngân hàng {externalTransactionId}",
            ReceivedBy = SystemUserId,
            ReceivedByName = "Bank Webhook",
            PaymentDate = receivedAt
        };

        order.PaidAmount += amount;
        order.DebtAmount = Math.Max(0, order.FinalAmount - order.PaidAmount);
        order.PaymentMethod = "Chuyển khoản";
        order.Status = order.DebtAmount == 0 ? "Paid" : "PartialPaid";

        if (customer != null)
        {
            customer.DebtAmount = Math.Max(0, customer.DebtAmount - amount);
        }

        if (!string.Equals(oldStatus, order.Status, StringComparison.OrdinalIgnoreCase))
        {
            order.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = order.Status,
                Note = $"Webhook ngân hàng xác nhận thanh toán {amount:N0} VND",
                ChangedBy = SystemUserId,
                ChangedByName = "Bank Webhook",
                CreatedAt = receivedAt
            });
        }

        return payment;
    }
}
