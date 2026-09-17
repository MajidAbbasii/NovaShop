namespace NovaShop.Application.Features.CustomDollRequests.Dtos;

public class CustomDollRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BodyColor { get; set; } = string.Empty;
    public string EyeColor { get; set; } = string.Empty;
    public int Height { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? AdminMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}