namespace NovaShop.Domain.Entities;

/// <summary>
/// Immutable ledger entry for every wallet balance change.
/// BalanceBefore/After keep the ledger self-consistent.
/// </summary>
public class WalletTransaction
{
    public const string TypeDeposit = "DEPOSIT";
    public const string TypePayment = "PAYMENT";
    public const string TypeRefund = "REFUND";
    public const string TypeReversal = "REVERSAL";
    public const string TypeAdjustment = "ADJUSTMENT";

    public const string StatusCompleted = "Completed";
    public const string StatusPending = "Pending";
    public const string StatusFailed = "Failed";

    public int Id { get; set; }
    public int WalletId { get; set; }
    public Wallet Wallet { get; init; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? PaymentId { get; set; }
    public string Status { get; set; } = StatusCompleted;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
