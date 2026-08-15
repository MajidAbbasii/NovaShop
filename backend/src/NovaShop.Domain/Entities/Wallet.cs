namespace NovaShop.Domain.Entities;

/// <summary>
/// Customer wallet. Balance is always in Toman (IRT).
/// Every balance change must create a WalletTransaction ledger entry.
/// </summary>
public class Wallet
{
    public const string CurrencyToman = "IRT";

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; init; } = null!;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = CurrencyToman;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<WalletTransaction> Transactions { get; private set; } = new();

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ کیف پول باید بزرگ‌تر از صفر باشد");
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ کیف پول باید بزرگ‌تر از صفر باشد");
        if (Balance < amount)
            throw new InvalidOperationException("موجودی کیف پول کافی نیست");
        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
