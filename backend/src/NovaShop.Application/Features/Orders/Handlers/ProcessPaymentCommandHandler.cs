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
using NovaShop.Common.Models;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResultDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IWalletService _walletService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        NovaShopDbContext context,
        IPaymentGateway paymentGateway,
        IWalletService walletService,
        IPublishEndpoint publishEndpoint,
        INotificationService notificationService,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _walletService = walletService;
        _publishEndpoint = publishEndpoint;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PaymentResultDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Load order + items + payment
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException("سفارش یافت نشد");

        // 2. Authorization check
        if (order.UserId != request.UserId)
            throw new InvalidOperationException("شما دسترسی به این سفارش ندارید");

        // 2b. Temporary business mode: online payment disabled → only wallet methods
        //     may proceed; gateway/COD initiation is rejected server-side.
        if (!PaymentPolicy.OnlinePaymentEnabled && order.PaymentMethod is not ("Wallet" or "WalletAndOnline"))
            throw new InvalidOperationException("پرداخت آنلاین موقتاً غیرفعال است؛ این سفارش با روش پرداخت حضوری ثبت شده است");

        // 3. State validation
        if (!order.CanBePaid)
        {
            if (order.Payment?.Status == "Completed")
            {
                _logger.LogInformation("Payment idempotent hit for order {OrderId}", order.Id);
                return new PaymentResultDto
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status,
                    PaymentStatus = order.Payment.Status,
                    TransactionId = order.Payment.TransactionId,
                    Success = true,
                    WalletBalance = order.PaymentMethod == "Wallet" || order.PaymentMethod == "WalletAndOnline"
                        ? (await _walletService.GetOrCreateWalletAsync(order.UserId, cancellationToken)).Balance
                        : null
                };
            }

            throw new InvalidOperationException(
                $"وضعیت سفارش '{order.Status}' اجازه پرداخت را نمی‌دهد");
        }

        // 4. Idempotency check on payment — prevents duplicate gateway initiations
        //    for the same logical payment attempt (Pending = already initiated,
        //    Completed = already finalized).
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.IdempotencyKey == request.IdempotencyKey &&
                    p.OrderId == request.OrderId, cancellationToken);

            if (existingPayment != null && existingPayment.Status is "Completed" or "Pending")
            {
                _logger.LogInformation(
                    "Payment idempotency key {Key} already processed for order {OrderId}",
                    request.IdempotencyKey, request.OrderId);

                return new PaymentResultDto
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status,
                    PaymentStatus = existingPayment.Status,
                    TransactionId = existingPayment.TransactionId,
                    Authority = existingPayment.IdempotencyKey,
                    Success = true
                };
            }
        }

        // 5. Route by payment method
        var paymentMethod = order.PaymentMethod ?? "CreditCard";
        var wallet = await _walletService.GetOrCreateWalletAsync(order.UserId, cancellationToken);

        if (paymentMethod == "Wallet")
        {
            if (wallet.Balance < order.TotalAmount)
                throw new InvalidOperationException(
                    $"موجودی کیف پول کافی نیست. موجودی: {wallet.Balance:N0} تومان، مبلغ سفارش: {order.TotalAmount:N0} تومان");

            return await PayWithWalletAsync(order, order.TotalAmount, null, wallet.Balance, cancellationToken);
        }

        if (paymentMethod == "WalletAndOnline")
        {
            var walletContribution = Math.Min(wallet.Balance, order.TotalAmount);
            if (walletContribution <= 0)
                throw new InvalidOperationException("موجودی کیف پول صفر است. ابتدا کیف پول را شارژ کنید یا روش پرداخت دیگر را انتخاب کنید.");

            if (walletContribution >= order.TotalAmount)
            {
                // Wallet fully covers it — treat as pure wallet payment.
                return await PayWithWalletAsync(order, order.TotalAmount, null, wallet.Balance, cancellationToken);
            }

            return await PayWithWalletAsync(order, walletContribution, paymentMethod, wallet.Balance, cancellationToken);
        }

        // 6. Online payment (CreditCard / BankTransfer / etc.) — initiate gateway redirect
        var callback = request.CallbackUrl
            ?? $"http://localhost:3000/api/payments/verify?orderId={order.Id}";

        var result = await _paymentGateway.InitiatePaymentAsync(
            paymentMethod,
            order.TotalAmount,
            "IRT",
            callback,
            $"order-{order.Id}",
            cancellationToken);

        if (!result.Success || string.IsNullOrEmpty(result.Authority))
        {
            order.Fail();
            if (order.Payment != null) order.Payment.Status = "Failed";
            await _context.SaveChangesAsync(cancellationToken);
            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Failed",
                Success = false,
                FailureReason = result.FailureReason
            };
        }

        // Persist authority reference on the payment row
        if (order.Payment != null)
        {
            order.Payment.IdempotencyKey = result.Authority;
            order.Payment.TransactionId = result.TransactionId;
            order.Payment.Status = "Pending";
        }
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentResultDto
        {
            OrderId = order.Id,
            OrderStatus = order.Status,
            PaymentStatus = "Pending",
            Success = true,
            RedirectUrl = result.RedirectUrl,
            Authority = result.Authority,
            OnlineAmount = order.TotalAmount,
            WalletBalance = wallet.Balance
        };
    }

    private async Task<PaymentResultDto> PayWithWalletAsync(
        Order order, decimal walletAmount, string? originalMethod,
        decimal walletBalanceBefore, CancellationToken cancellationToken)
    {
        if (walletAmount > 0)
        {
            // Debit wallet FIRST (in this same transaction when full payment).
            // For partial (WalletAndOnline) the wallet debit is committed here;
            // if the online remainder fails, VerifyPaymentCommandHandler performs
            // a wallet REVERSAL to give the money back.
            var wallet = await _walletService.DebitAsync(
                order.UserId,
                walletAmount,
                WalletTransaction.TypePayment,
                walletAmount >= order.TotalAmount
                    ? $"پرداخت سفارش {order.Id}"
                    : $"پرداخت بخشی از سفارش {order.Id}",
                reference: $"order-{order.Id}",
                orderId: order.Id,
                ct: cancellationToken);

            // Partial → wallet debited; order must NOT be paid yet.
            if (walletAmount < order.TotalAmount && originalMethod == "WalletAndOnline")
            {
                order.Confirm();

                if (order.Payment != null)
                {
                    order.Payment.WalletAmount = walletAmount;
                    order.Payment.Status = "Pending";
                }

                await _context.SaveChangesAsync(cancellationToken);

                var result = await _paymentGateway.InitiatePaymentAsync(
                    "CreditCard",
                    order.TotalAmount - walletAmount,
                    "IRT",
                    $"http://localhost:3000/api/payments/verify",
                    $"order-{order.Id}-remainder",
                    cancellationToken);

                return new PaymentResultDto
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status,
                    PaymentStatus = "Pending",
                    Success = true,
                    RedirectUrl = result.RedirectUrl,
                    Authority = result.Authority,
                    OnlineAmount = order.TotalAmount - walletAmount,
                    WalletAmount = walletAmount,
                    WalletBalance = wallet.Balance
                };
            }
        }
        else
        {
            // Wallet covers nothing (shouldn't happen — validated upstream)
            order.Confirm();
        }

        // FULL WALLET PAYMENT (walletAmount >= order.TotalAmount)
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (order.Payment == null)
                throw new InvalidOperationException("رکورد پرداخت سفارش یافت نشد");

            if (order.Payment.Status == "Completed")
                throw new InvalidOperationException("این سفارش قبلاً پرداخت شده است");

            order.Confirm();
            order.MarkAsPaid();

            order.Payment.Status = "Completed";
            order.Payment.TransactionId = $"WALLET-{Guid.NewGuid():N}"[..20];
            order.Payment.IdempotencyKey ??= $"wallet-{order.Id}-{Guid.NewGuid():N}"[..32];
            if (order.Payment.WalletAmount == 0) order.Payment.WalletAmount = walletAmount;

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
                        Reference = $"paid-wallet-order-{order.Id}"
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            try
            {
                await _notificationService.NotifyPaymentSuccessfulAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment-success notification for order {OrderId}", order.Id);
            }

            var walletAfter = await _walletService.GetOrCreateWalletAsync(order.UserId, cancellationToken);

            return new PaymentResultDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                PaymentStatus = "Paid",
                TransactionId = order.Payment.TransactionId,
                Success = true,
                WalletAmount = walletAmount,
                WalletBalance = walletAfter.Balance
            };
        }
        catch
        {
            _logger.LogError("Wallet payment failed for order {OrderId}. Rolling back.", order.Id);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}