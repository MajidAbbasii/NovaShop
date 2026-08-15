using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

/// <summary>
/// Initiate a wallet recharge. Returns a gateway RedirectUrl; the balance only
/// increases after VerifyWalletChargeCommand (server-side verification).
/// </summary>
public class ChargeWalletCommandHandler : IRequestHandler<ChargeWalletCommand, WalletChargeResultDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IWalletService _walletService;
    private readonly ILogger<ChargeWalletCommandHandler> _logger;

    public ChargeWalletCommandHandler(
        NovaShopDbContext context,
        IPaymentGateway paymentGateway,
        IWalletService walletService,
        ILogger<ChargeWalletCommandHandler> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<WalletChargeResultDto> Handle(ChargeWalletCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetOrCreateWalletAsync(request.UserId, cancellationToken);

        var authority = $"CHARGE-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

        // Persist the recharge intent so verification can find it.
        _context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = request.Amount,
            BalanceBefore = wallet.Balance,
            BalanceAfter = wallet.Balance,
            Type = WalletTransaction.TypeDeposit,
            Description = $"شارژ کیف پول (در انتظار پرداخت) {request.Amount:N0} تومان",
            Reference = authority,
            Status = WalletTransaction.StatusPending
        });
        await _context.SaveChangesAsync(cancellationToken);

        var callback = request.CallbackUrl ?? "http://localhost:3000/api/wallet/verify";
        var result = await _paymentGateway.InitiatePaymentAsync(
            "Online",
            request.Amount,
            "IRT",
            callback,
            authority,
            cancellationToken);

        return new WalletChargeResultDto
        {
            WalletId = wallet.Id,
            Amount = request.Amount,
            Balance = wallet.Balance,
            Success = result.Success,
            RedirectUrl = result.RedirectUrl,
            Authority = result.Authority ?? authority,
            FailureReason = result.Success ? null : result.FailureReason
        };
    }
}

/// <summary>
/// Server-side verification of a wallet recharge. Only this handler credits
/// the wallet — never the browser redirect. Duplicate callbacks are idempotent.
/// </summary>
public class VerifyWalletChargeCommandHandler : IRequestHandler<VerifyWalletChargeCommand, WalletChargeResultDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IWalletService _walletService;
    private readonly ILogger<VerifyWalletChargeCommandHandler> _logger;

    public VerifyWalletChargeCommandHandler(
        NovaShopDbContext context,
        IPaymentGateway paymentGateway,
        IWalletService walletService,
        ILogger<VerifyWalletChargeCommandHandler> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<WalletChargeResultDto> Handle(VerifyWalletChargeCommand request, CancellationToken cancellationToken)
    {
        var pending = await _context.WalletTransactions
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t =>
                t.Reference == request.Authority &&
                t.Type == WalletTransaction.TypeDeposit &&
                t.Status == WalletTransaction.StatusPending, cancellationToken);

        if (pending == null)
        {
            // Maybe already completed (duplicate callback)
            var completed = await _context.WalletTransactions
                .FirstOrDefaultAsync(t =>
                    t.Reference == request.Authority && t.Type == WalletTransaction.TypeDeposit,
                    cancellationToken);

            if (completed != null && completed.Status == WalletTransaction.StatusCompleted)
            {
                return new WalletChargeResultDto
                {
                    WalletId = completed.WalletId,
                    Amount = completed.Amount,
                    Balance = completed.BalanceAfter,
                    Success = true
                };
            }

            _logger.LogWarning("Wallet charge verify: unknown authority {Authority}", request.Authority);
            return new WalletChargeResultDto { Success = false, FailureReason = "شارژ یافت نشد" };
        }

        var verification = await _paymentGateway.VerifyPaymentAsync(
            "Online",
            request.Authority,
            pending.Amount,
            "IRT",
            cancellationToken);

        if (!verification.Success)
        {
            pending.Status = WalletTransaction.StatusFailed;
            pending.FailureReason = verification.FailureReason;
            await _context.SaveChangesAsync(cancellationToken);
            return new WalletChargeResultDto
            {
                WalletId = pending.WalletId,
                Amount = pending.Amount,
                Balance = pending.BalanceAfter,
                Success = false,
                FailureReason = verification.FailureReason
            };
        }

        // Credit the wallet (creates the completed ledger entry in the same transaction).
        var wallet = await _walletService.CreditAsync(
            pending.Wallet.UserId,
            pending.Amount,
            WalletTransaction.TypeDeposit,
            $"شارژ کیف پول {pending.Amount:N0} تومان",
            reference: pending.Reference,
            ct: cancellationToken);

        // Mark the pending row complete
        pending.Status = WalletTransaction.StatusCompleted;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet charged userId={UserId} amount={Amount} authority={Authority}",
            pending.Wallet.UserId, pending.Amount, request.Authority);

        return new WalletChargeResultDto
        {
            WalletId = wallet.Id,
            Amount = pending.Amount,
            Balance = wallet.Balance,
            Success = true
        };
    }
}