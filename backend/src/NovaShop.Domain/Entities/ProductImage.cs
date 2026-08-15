namespace NovaShop.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductColorId { get; set; }
    public ProductColor? ProductColor { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ProductColor
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<ProductImage> Images { get; private set; } = new();

    public void ReserveStock(int quantity)
    {
        if (Stock < quantity)
            throw new InvalidOperationException($"رنگ {Name} فقط {Stock} عدد موجودی دارد (درخواست: {quantity})");
        Stock -= quantity;
    }

    public void ReleaseStock(int quantity)
    {
        Stock += quantity;
    }
}
