namespace NovaShop.Application.Features.Products.Dtos;

/// <summary>Product search result with highlighted snippet.</summary>
public class ProductSearchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Highlighted description snippet around match.</summary>
    public string? Description { get; set; }
    /// <summary>Full description (for detail view).</summary>
    public string? FullDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }
    /// <summary>Relevance rank from full-text query.</summary>
    public int? Rank { get; set; }
}

/// <summary>Autocomplete/suggestion result.</summary>
public class ProductSuggestion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
