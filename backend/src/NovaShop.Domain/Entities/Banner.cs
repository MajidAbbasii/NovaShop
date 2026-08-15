namespace NovaShop.Domain.Entities;

/// <summary>
/// Storefront hero banner (admin-managed). Public GET /api/banners returns
/// active banners ordered by SortOrder.
/// </summary>
public class Banner
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty; // e.g. /products or /products?category=2
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}