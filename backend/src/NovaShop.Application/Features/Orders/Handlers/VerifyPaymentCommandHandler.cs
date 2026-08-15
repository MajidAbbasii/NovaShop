using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Messages;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

/// <summary>
/// Server-side payment verification. Marking an order Paid is ONLY possible
/// through this handler (or the wallet path in ProcessPaymentCommandHandler).
/// The browser redirect alone never marks an order paid.
/// </summary>
public class VerifyPaymentCommandHandler : IRequestHandler<VerifyPaymentCommand, PaymentResultDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IWalletService _walletService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VerifyPaymentCommandHandler> _logger;

    public VerifyPaymentCommandHandler(
        NovaShopDbContext context,
        IPaymentGateway paymentGateway,
        IWalletService walletService,
        IPublishEndpoint publishEndpoint,
        INotificationService notificationService,
        ILogger<VerifyPaymentCommandHandler> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _walletService = walletService;
        _publishEndpoint = publishEndpoint;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PaymentResultDto> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Find the payment row by authority (gateway idempotency reference)
        var payment = await _context.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.IdempotencyKey == request.Authority, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("Verify: unknown authority {Authority}", request.Authority);
            return new PaymentResultDto
            {
                Success = false,
                FailureReason = "پرداخت یافت نشد"
            };
        }

        var order = payment.Order;

        // 2. Already paid → idempotent success (duplicate callback protection)
        if (order.PaymentStatus == Order.PaymentPaid || payment.Status == "Completed")
        {
            _logger.LogInformation("Verify: order {OrderId} already paid — duplicate callback ignored", order.Id);
            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Paid",
                TransactionId = payment.TransactionId,
                Success = true
            };
        }

        // 3. Expired reservation → reject
        if (order.ReservationExpiresAt.HasValue && order.ReservationExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Verify: order {OrderId} reservation expired at {ExpiresAt}",
                order.Id, order.ReservationExpiresAt.Value);
            payment.Status = "Expired";
            await _context.SaveChangesAsync(cancellationToken);
            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Expired",
                Success = false,
                FailureReason = "مهلت پرداخت سفارش به پایان رسیده است"
            };
        }

        // 4. Server-side verification with the gateway (authority + exact amount check)
        var expectedAmount = payment.Amount;
        var verification = await _paymentGateway.VerifyPaymentAsync(
            payment.PaymentMethod,
            request.Authority,
            expectedAmount,
            "IRT",
            cancellationToken);

        // 5. Handle verification result inside a transaction
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!verification.Success)
            {
                order.Fail();
                payment.Status = "Failed";
                payment.TransactionId = string.Empty;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new PaymentResultDto
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status,
                    PaymentStatus = "Failed",
                    Success = false,
                    FailureReason = verification.FailureReason
                };
            }

            // SUCCESS — mark paid, confirm reservations (permanent stock deduction)
            var wasWalletAndOnline = order.PaymentMethod == "WalletAndOnline";

            order.Confirm();
            order.MarkAsPaid();
            payment.Status = "Completed";
            payment.TransactionId = verification.TransactionId;

            foreach (var item in order.Items)
            {
                item.Product?.ConfirmReservation();
                if (item.Product != null)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        OrderId = order.Id,
                        Type = InventoryTransaction.TypeConfirm,
                        Quantity = item.Quantity,
                        StockBefore = item.Product.StockBefore,
                        StockAfter = item.Product.StockAfter,
                        Reference = $"paid-online-order-{order.Id}"
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _publishEndpoint.Publish(new PaymentCompletedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod ?? "Online",
                TransactionId = verification.TransactionId,
                CompletedAt = DateTime.UtcNow
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Notifications after commit
            try
            {
                await _notificationService.NotifyPaymentSuccessfulAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment-success notification for order {OrderId}", order.Id);
            }

            _logger.LogInformation(
                "Order {OrderId} PAID after server-side verification. Authority: {Authority}, Txn: {TxnId}",
                order.Id, request.Authority, verification.TransactionId);

            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Paid",
                TransactionId = verification.TransactionId,
                Success = true
            };
        }
        catch
        {
            _logger.LogError("Payment verification failed for order {OrderId}. Rolling back.", order.Id);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}