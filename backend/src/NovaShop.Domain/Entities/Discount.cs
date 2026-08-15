namespace NovaShop.Domain.Entities;

public class Discount
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; } // Percentage (0-100) or Fixed amount
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public List<int> ApplicableProductIds { get; set; } = new();
    public List<int> ApplicableCategoryIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsValid(DateTime currentDate)
    {
        if (!IsActive) return false;
        if (currentDate < StartDate || currentDate > EndDate) return false;
        if (UsedCount >= UsageLimit) return false;
        return true;
    }

    public bool IsApplicableToProduct(int productId, List<int> productCategoryIds, decimal orderTotal)
    {
        if (!IsValid(DateTime.UtcNow)) return false;
        if (orderTotal < MinOrderAmount) return false;

        // Check specific products first
        if (ApplicableProductIds.Any() && !ApplicableProductIds.Contains(productId))
            return false;

        // Then check categories
        if (ApplicableCategoryIds.Any())
        {
            var productCategories = productCategoryIds;
            if (!productCategories.Any() || !ApplicableCategoryIds.Any(catId => productCategories.Contains(catId)))
                return false;
        }

        return true;
    }

    public void IncrementUsage()
    {
        UsedCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CalculateDiscount(decimal amount)
    {
        return Type switch
        {
            DiscountType.Percentage => (amount * Value) / 100,
            DiscountType.Fixed => Math.Min(Value, amount),
            _ => 0m
        };
    }
}