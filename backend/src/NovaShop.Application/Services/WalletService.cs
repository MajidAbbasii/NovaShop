using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Services;

/// <summary>
/// Wallet service. All balance changes go through here and produce a
/// WalletTransaction ledger entry inside the same DB transaction, so the
/// balance and the ledger can never diverge.
/// </summary>
public interface IWalletService
{
    Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken ct = default);
    Task<Wallet> CreditAsync(int userId, decimal amount, string type, string description,
        string? reference = null, int? orderId = null, int? paymentId = null, CancellationToken ct = default);
    Task<Wallet> DebitAsync(int userId, decimal amount, string type, string description,
        string? reference = null, int? orderId = null, int? paymentId = null, CancellationToken ct = default);
}

public class WalletService : IWalletService
{
    private readonly NovaShopDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(NovaShopDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken ct = default)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet != null) return wallet;

        wallet = new Wallet { UserId = userId, Balance = 0m, Currency = Wallet.CurrencyToman };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync(ct);
        return wallet;
    }

    public async Task<Wallet> CreditAsync(int userId, decimal amount, string type, string? description,
        string? reference = null, int? orderId = null, int? paymentId = null, CancellationToken ct = default)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct)
            ?? await GetOrCreateWalletAsync(userId, ct);

        var before = wallet.Balance;
        wallet.Credit(amount);

        _context.WalletTransactions.Add(new WalletTransaction
        {
            Wallet = wallet,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            Type = type,
            Description = description ?? string.Empty,
            Reference = reference,
            OrderId = orderId,
            PaymentId = paymentId,
            Status = WalletTransaction.StatusCompleted
        });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Wallet credit userId={UserId} amount={Amount} type={Type}", userId, amount, type);
        return wallet;
    }

    public async Task<Wallet> DebitAsync(int userId, decimal amount, string type, string description,
        string? reference = null, int? orderId = null, int? paymentId = null, CancellationToken ct = default)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct)
            ?? await GetOrCreateWalletAsync(userId, ct);

        if (wallet.Balance < amount)
            throw new InvalidOperationException("موجودی کیف پول کافی نیست");

        var before = wallet.Balance;
        wallet.Debit(amount);

        _context.WalletTransactions.Add(new WalletTransaction
        {
            Wallet = wallet,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            Type = type,
            Description = description,
            Reference = reference,
            OrderId = orderId,
            PaymentId = paymentId,
            Status = WalletTransaction.StatusCompleted
        });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Wallet debit userId={UserId} amount={Amount} type={Type}", userId, amount, type);
        return wallet;
    }
}