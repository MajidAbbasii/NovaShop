using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NovaShop.Domain.Exceptions;

namespace NovaShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double Rating { get; init; } = 4.0;
    public int Stock { get; set; }
    public bool IsAvailable => Stock > 0;

    // Concurrency token — prevents lost updates (double-reserve / double-confirm)
    // under concurrent checkouts. SQL Server rowversion advances on every write;
    // EF throws DbUpdateConcurrencyException on stale writes.
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public List<ProductImage> Images { get; private set; } = new();
    public List<ProductColor> Colors { get; private set; } = new();

    public string PrimaryImageUrl =>
        Images.FirstOrDefault(i => i.IsPrimary)?.Url
        ?? Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url
        ?? ImageUrl;

    public List<Review> Reviews { get; private set; } = new();

    // Checkout flow fields
    public DateTime? ReservedUntil { get; set; }
    public int ReservedQuantity { get; set; } = 0;

    // --- Inventory ledger helpers ---
    // Conceptual model:
    //   Stock          = available/sellable units
    //   ReservedQuantity = units promised to unpaid orders (locked, not sellable)
    //   Total physical = Stock + ReservedQuantity
    //
    // ReserveStock:        available -> reserved  (Stock -= qty, ReservedQuantity += qty)
    // ConfirmReservation:  reserved -> sold      (ReservedQuantity -= qty; Stock untouched — already deducted at reserve time)
    // ReleaseReservation:  reserved -> available (ReservedQuantity -= qty, Stock += qty)
    //
    // All operations are idempotent and guarded by state checks so double
    // application never double-spends or double-restores stock.

    /// <summary>
    /// Reserve <paramref name="quantity"/> units for an order. Stock is
    /// reduced and ReservedQuantity increased. Validates available stock on
    /// every call — concurrent callers racing the last unit will be blocked by
    /// the RowVersion concurrency token + SQL isolation.
    /// </summary>
    public void ReserveStock(int quantity, DateTime expiresAt)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (Stock < quantity)
            // Controlled 409 Conflict — not a 500.
            throw new InsufficientStockException(Name);

        Stock -= quantity;
        ReservedQuantity += quantity;
        ReservedUntil = expiresAt;
    }

    /// <summary>
    /// Finalize a previously-created reservation: the reserved units become
    /// permanently sold. Idempotent — a no-op when ReservedQuantity is already 0
    /// (duplicate/confirm protection).
    /// </summary>
    public void ConfirmReservation()
    {
        if (ReservedQuantity <= 0)
        {
            // Nothing reserved — idempotent success (duplicate confirm protection).
            StockBefore = Stock;
            StockAfter = Stock;
            ReservedUntil = null;
            return;
        }

        StockBefore = Stock;
        ReservedQuantity = 0;
        ReservedUntil = null;
        StockAfter = Stock;
    }

    /// <summary>
    /// Release the entire reservation back to available stock.
    /// Idempotent — safe when ReservedQuantity is already 0.
    /// </summary>
    public void ReleaseReservation()
    {
        if (ReservedQuantity <= 0)
        {
            StockBefore = Stock;
            StockAfter = Stock;
            ReservedUntil = null;
            return;
        }

        StockBefore = Stock;
        Stock += ReservedQuantity;
        ReservedQuantity = 0;
        ReservedUntil = null;
        StockAfter = Stock;
    }

    /// <summary>
    /// Release a partial reservation (per-order quantity) back to available
    /// stock. Used by cancellation/refund to avoid touching stock that belongs
    /// to OTHER orders. Idempotent and clamped to available reserved amount —
    /// never drives ReservedQuantity negative.
    /// </summary>
    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0) return;
        var releaseQty = Math.Min(quantity, ReservedQuantity);
        if (releaseQty <= 0) return;

        StockBefore = Stock;
        Stock += releaseQty;
        ReservedQuantity -= releaseQty;
        if (ReservedQuantity <= 0)
        {
            ReservedQuantity = 0;
            ReservedUntil = null;
        }
        StockAfter = Stock;
    }

    // For inventory ledger tracking (in-memory only, not persisted)
    [NotMapped]
    public int StockBefore { get; private set; }
    [NotMapped]
    public int StockAfter { get; private set; }
}
