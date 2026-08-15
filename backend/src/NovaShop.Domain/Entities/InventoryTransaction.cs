namespace NovaShop.Domain.Entities;

/// <summary>
/// Immutable ledger entry for every inventory movement (reserve, confirm, release).
/// </summary>
public class InventoryTransaction
{
    public const string TypeReserve = "Reserve";
    public const string TypeConfirm = "Confirm";
    public const string TypeRelease = "Release";

    public int Id { get; set; }
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;
    public int? OrderId { get; init; }
    public Order? Order { get; init; }
    public string Type { get; init; } = TypeReserve;
    public int Quantity { get; init; }
    public int StockBefore { get; init; }
    public int StockAfter { get; init; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
