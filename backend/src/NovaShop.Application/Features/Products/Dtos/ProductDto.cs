namespace NovaShop.Application.Features.Products.Dtos;

public class ProductImageDto
{
    public int Id { get; set; }
    public int? ProductColorId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductColorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HexCode { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductColorDto> Colors { get; set; } = new();
}
