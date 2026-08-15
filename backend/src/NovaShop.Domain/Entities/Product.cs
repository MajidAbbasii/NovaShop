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

    // Domain Methods for checkout
    public void ReserveStock(int quantity, DateTime expiresAt)
    {
        if (Stock < quantity)
            throw new InvalidOperationException($"محصول {Name} فقط {Stock} عدد موجودی دارد (درخواست: {quantity})");

        StockBefore = Stock;
        Stock -= quantity;
        ReservedQuantity += quantity;
        ReservedUntil = expiresAt;
        StockAfter = Stock;
    }

    public void ConfirmReservation()
    {
        // Convert reserved stock to permanent (release holds if any)
        ReservedQuantity = 0;
        ReservedUntil = null;
    }

    public void ReleaseReservation()
    {
        StockBefore = Stock;
        Stock += ReservedQuantity;
        ReservedQuantity = 0;
        ReservedUntil = null;
        StockAfter = Stock;
    }

    // For inventory ledger tracking (in-memory only, not persisted)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int StockBefore { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int StockAfter { get; private set; }
}
