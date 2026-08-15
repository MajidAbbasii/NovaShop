namespace NovaShop.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; init; }
    public Order Order { get; init; } = null!;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string TransactionId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    /// <summary>Wallet contribution when PaymentMethod is WalletAndOnline.</summary>
    public decimal WalletAmount { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
