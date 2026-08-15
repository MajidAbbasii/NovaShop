using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Messages;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

/// <summary>
/// Refunds a paid order to the customer wallet. Idempotent:
/// - Order.RefundProcessed guard prevents double refund.
/// - Wallet transaction reference is order-scoped.
/// Only orders that were paid via wallet or online can be refunded to the wallet.
/// </summary>
public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, PaymentResultDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IWalletService _walletService;
    private readonly INotificationService _notificationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RefundOrderCommandHandler> _logger;

    public RefundOrderCommandHandler(
        NovaShopDbContext context,
        IWalletService walletService,
        INotificationService notificationService,
        IPublishEndpoint publishEndpoint,
        ILogger<RefundOrderCommandHandler> logger)
    {
        _context = context;
        _walletService = walletService;
        _notificationService = notificationService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<PaymentResultDto> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException("سفارش یافت نشد");

        if (!order.CanRefund)
            throw new InvalidOperationException(
                $"سفارش {order.Id} قابل بازگشت وجه نیست (وضعیت: {order.Status}, پرداخت: {order.PaymentStatus})");

        var refundAmount = request.Amount ?? order.TotalAmount;
        if (refundAmount <= 0 || refundAmount > order.TotalAmount)
            throw new InvalidOperationException("مبلغ بازگشت وجه نامعتبر است");

        // Duplicate-refund guard (defense in depth beyond Order.RefundProcessed)
        var existingRefund = await _context.WalletTransactions
            .FirstOrDefaultAsync(t =>
                t.OrderId == order.Id && t.Type == WalletTransaction.TypeRefund,
                cancellationToken);
        if (existingRefund != null)
            throw new InvalidOperationException("این سفارش قبلاً بازگشت وجه شده است");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Credit wallet with a REFUND ledger entry inside the transaction
            await _walletService.CreditAsync(
                order.UserId,
                refundAmount,
                WalletTransaction.TypeRefund,
                $"بازگشت وجه سفارش {order.Id} ({(request.Reason ?? "لغو سفارش")})",
                reference: $"refund-order-{order.Id}",
                orderId: order.Id,
                ct: cancellationToken);

            order.MarkAsRefunded(refundAmount, request.Reason ?? "لغو سفارش");
            if (order.Payment != null)
            {
                order.Payment.Status = "Refunded";
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            try
            {
                await _notificationService.NotifyOrderStatusChangedAsync(order, Order.StatusRefunded, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send refund notification for order {OrderId}", order.Id);
            }

            await _publishEndpoint.Publish(new OrderRefundedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = refundAmount,
                RefundedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Order {OrderId} refunded amount={Amount} to wallet", order.Id, refundAmount);

            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Refunded",
                TransactionId = $"REFUND-{order.Id}",
                Success = true
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}