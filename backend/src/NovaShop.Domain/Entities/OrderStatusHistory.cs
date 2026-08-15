namespace NovaShop.Domain.Entities;

/// <summary>
/// Audit trail entry recording every status change an order goes through.
/// </summary>
public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; init; }
    public Order Order { get; init; } = null!;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ChangedByUserId { get; set; }
    public string ChangedByRole { get; set; } = "System";
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
