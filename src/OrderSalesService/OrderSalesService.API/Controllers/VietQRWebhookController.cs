using System.Security.Cryptography;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Features.Payments.BankTransfers;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;
using SharedContracts.Events;

namespace OrderSalesService.API.Controllers;

/// <summary>
/// Receives bank transfer callbacks from VietQR/SePay and settles matching orders.
/// The transfer content must contain the order code, for example: DH-20260529183000123.
/// </summary>
[ApiController]
[Route("vqr")]
public class VietQRWebhookController : ControllerBase
{
    private const string ConsumerName = "BankTransferWebhook";
    private static readonly object TransactionsLock = new();

    private readonly ILogger<VietQRWebhookController> _logger;
    private readonly IOrderSalesDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public static readonly List<VietQRTransaction> ReceivedTransactions = new();

    public VietQRWebhookController(ILogger<VietQRWebhookController> logger, IOrderSalesDbContext context, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// POST /vqr/api/token_generate - VietQR token endpoint.
    /// </summary>
    [HttpPost("api/token_generate")]
    public IActionResult GetToken([FromHeader(Name = "Authorization")] string? authorization)
    {
        _logger.LogInformation("VietQR token request received. Auth header present: {HasAuth}", !string.IsNullOrWhiteSpace(authorization));

        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { code = "401", message = "Unauthorized", data = (object?)null });
        }

        try
        {
            var base64 = authorization["Basic ".Length..].Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                return Unauthorized(new { code = "401", message = "Invalid credentials", data = (object?)null });
            }

            var username = parts[0];
            var password = parts[1];

            if ((username == "customer-giangmart-user26591" && password == "Y3VzdG9tZXItZlhbmdtYXJ0LXVzZXIyNjU5MQ==") ||
                (username == "giangmart" && password == "giangmart@2026"))
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{DateTime.UtcNow.Ticks}"));

                _logger.LogInformation("VietQR token generated successfully for user: {User}", username);

                return Ok(new
                {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = 3600,
                    code = "00",
                    message = "success"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing VietQR Basic Auth credentials.");
        }

        return Unauthorized(new { code = "401", message = "Invalid credentials", data = (object?)null });
    }

    /// <summary>
    /// POST /vqr/bank/api/test/transaction-callback - VietQR transaction callback.
    /// </summary>
    [HttpPost("bank/api/test/transaction-callback")]
    public async Task<IActionResult> TransactionCallback([FromBody] VietQRCallbackRequest request)
    {
        _logger.LogInformation(
            "VietQR transaction callback received. Amount: {Amount}, Content: {Content}, TransactionId: {TxnId}",
            request.Amount, request.Content, request.TransactionId);

        var transaction = new VietQRTransaction
        {
            TransactionId = request.TransactionId ?? Guid.NewGuid().ToString("N"),
            Amount = request.Amount,
            Content = request.Content ?? "",
            BankAccount = request.BankAccount ?? "",
            TransactionDate = request.TransactionDate ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ReceivedAt = DateTime.Now,
            ReferenceNumber = request.ReferenceNumber ?? ""
        };

        StoreTransaction(transaction);

        var settlement = await TrySettleBankTransferAsync(
            provider: "VietQR",
            externalTransactionId: transaction.TransactionId,
            amount: transaction.Amount,
            content: transaction.Content,
            receivedAt: transaction.ReceivedAt);

        return Ok(new
        {
            code = "00",
            message = "success",
            data = settlement
        });
    }

    /// <summary>
    /// POST /vqr/sepay/webhook - SePay balance-change webhook.
    /// </summary>
    [HttpPost("sepay/webhook")]
    public async Task<IActionResult> SePayCallback([FromBody] SePayWebhookRequest request)
    {
        _logger.LogInformation(
            "SePay transaction callback received. Amount: {Amount}, Content: {Content}, ReferenceCode: {RefCode}",
            request.TransferAmount, request.Content, request.ReferenceCode);

        var transferContent = FirstNonEmpty(request.Content, request.Description, request.Code, request.ReferenceCode);

        var transaction = new VietQRTransaction
        {
            TransactionId = request.ReferenceCode ?? request.Id.ToString(),
            Amount = request.TransferAmount,
            Content = transferContent,
            BankAccount = request.AccountNumber ?? "",
            TransactionDate = request.TransactionDate ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ReceivedAt = DateTime.Now,
            ReferenceNumber = request.ReferenceCode ?? ""
        };

        StoreTransaction(transaction);

        var settlement = await TrySettleBankTransferAsync(
            provider: "SePay",
            externalTransactionId: transaction.TransactionId,
            amount: transaction.Amount,
            content: transaction.Content,
            receivedAt: transaction.ReceivedAt);

        return Ok(new { success = true, settlement });
    }

    /// <summary>
    /// GET /vqr/transactions - Returns recent webhook transactions for frontend polling/debugging.
    /// </summary>
    [HttpGet("transactions")]
    public IActionResult GetTransactions([FromQuery] string? orderCode)
    {
        if (!string.IsNullOrEmpty(orderCode))
        {
            var matched = SnapshotTransactions()
                .Where(t => t.Content.Contains(orderCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.ReceivedAt)
                .ToList();

            return Ok(new { code = "00", message = "success", data = matched });
        }

        var recent = SnapshotTransactions()
            .OrderByDescending(t => t.ReceivedAt)
            .Take(50)
            .ToList();

        return Ok(new { code = "00", message = "success", data = recent });
    }

    /// <summary>
    /// GET /vqr/health - Health check for webhook setup.
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            code = "00",
            message = "Bank transfer webhook is running",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalTransactions = GetTransactionCount()
        });
    }

    private async Task<BankTransferSettlementResult> TrySettleBankTransferAsync(
        string provider,
        string externalTransactionId,
        decimal amount,
        string content,
        DateTime receivedAt)
    {
        if (amount <= 0)
        {
            _logger.LogWarning("{Provider} webhook ignored because amount is not positive. TransactionId: {TransactionId}, Amount: {Amount}",
                provider, externalTransactionId, amount);
            return BankTransferSettlementResult.Ignored("Số tiền giao dịch không hợp lệ.");
        }

        var orderCode = BankTransferPaymentMatcher.ExtractOrderCode(content);
        if (orderCode == null)
        {
            _logger.LogWarning("{Provider} webhook received but no order code was found in content: {Content}",
                provider, content);
            return BankTransferSettlementResult.Unmatched("Không tìm thấy mã đơn DH-... trong nội dung chuyển khoản.");
        }

        var eventId = BuildProcessedEventId(provider, externalTransactionId);
        var alreadyProcessed = await _context.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId && e.ConsumerName == ConsumerName);

        if (alreadyProcessed)
        {
            _logger.LogInformation("{Provider} webhook transaction {TransactionId} was already processed for order {OrderCode}.",
                provider, externalTransactionId, orderCode);
            return BankTransferSettlementResult.AlreadyProcessed(orderCode);
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderStatusHistories)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
        {
            _logger.LogWarning("{Provider} webhook matched order code {OrderCode}, but the order was not found in database.",
                provider, orderCode);
            return BankTransferSettlementResult.Unmatched($"Không tìm thấy đơn hàng {orderCode} trong hệ thống.");
        }

        if (order.DebtAmount <= 0 || string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            _context.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = eventId,
                ConsumerName = ConsumerName,
                ProcessedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return BankTransferSettlementResult.AlreadyPaid(order.OrderCode, order.Id);
        }

        var payment = PaymentSettlement.ApplyBankTransfer(
            order,
            order.Customer,
            amount,
            externalTransactionId,
            receivedAt.ToUniversalTime());

        _context.PaymentTransactions.Add(payment);
        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = ConsumerName,
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            ServiceName = "OrderSalesService",
            Action = "BankTransferSettled",
            EntityType = "PaymentTransaction",
            EntityId = payment.Id.ToString(),
            Severity = "Info",
            Description = $"{provider} webhook settled order {order.OrderCode} with amount {amount}.",
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "{Provider} webhook settled order {OrderCode}. TransactionId: {TransactionId}, Amount: {Amount}, Status: {Status}, Debt: {Debt}",
            provider, order.OrderCode, externalTransactionId, amount, order.Status, order.DebtAmount);

        return BankTransferSettlementResult.Settled(order.OrderCode, order.Id, payment.Id, order.Status, order.DebtAmount);
    }

    private static string BuildProcessedEventId(string provider, string externalTransactionId)
    {
        var raw = $"{provider}:{externalTransactionId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"bank:{Convert.ToHexString(hash)[..32]}";
    }

    private static void StoreTransaction(VietQRTransaction transaction)
    {
        lock (TransactionsLock)
        {
            ReceivedTransactions.Add(transaction);
        }
    }

    private static List<VietQRTransaction> SnapshotTransactions()
    {
        lock (TransactionsLock)
        {
            return ReceivedTransactions.ToList();
        }
    }

    private static int GetTransactionCount()
    {
        lock (TransactionsLock)
        {
            return ReceivedTransactions.Count;
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public record BankTransferSettlementResult(
    string Status,
    string Message,
    string? OrderCode = null,
    Guid? OrderId = null,
    Guid? PaymentId = null,
    string? OrderStatus = null,
    decimal? RemainingDebt = null)
{
    public static BankTransferSettlementResult Settled(string orderCode, Guid orderId, Guid paymentId, string orderStatus, decimal remainingDebt) =>
        new("settled", "Đã tự động ghi nhận thanh toán cho đơn hàng.", orderCode, orderId, paymentId, orderStatus, remainingDebt);

    public static BankTransferSettlementResult AlreadyProcessed(string orderCode) =>
        new("already_processed", "Giao dịch webhook này đã được xử lý trước đó.", orderCode);

    public static BankTransferSettlementResult AlreadyPaid(string orderCode, Guid orderId) =>
        new("already_paid", "Đơn hàng đã được thanh toán trước đó.", orderCode, orderId);

    public static BankTransferSettlementResult Unmatched(string message) =>
        new("unmatched", message);

    public static BankTransferSettlementResult Ignored(string message) =>
        new("ignored", message);
}

public class VietQRCallbackRequest
{
    public decimal Amount { get; set; }
    public string? Content { get; set; }
    public string? TransactionId { get; set; }
    public string? BankAccount { get; set; }
    public string? TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? BankCode { get; set; }
    public string? SubAccount { get; set; }
}

public class VietQRTransaction
{
    public string TransactionId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Content { get; set; } = "";
    public string BankAccount { get; set; } = "";
    public string TransactionDate { get; set; } = "";
    public DateTime ReceivedAt { get; set; }
    public string ReferenceNumber { get; set; } = "";
}

public class SePayWebhookRequest
{
    public long Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? Code { get; set; }
    public string? Content { get; set; }
    public string? TransferType { get; set; }
    public string? Description { get; set; }
    public decimal TransferAmount { get; set; }
    public string? ReferenceCode { get; set; }
}
