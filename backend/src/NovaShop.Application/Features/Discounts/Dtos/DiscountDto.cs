namespace NovaShop.Application.Features.Discounts.Dtos;

public class DiscountDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public List<int> ApplicableProductIds { get; set; } = new();
    public List<int> ApplicableCategoryIds { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
