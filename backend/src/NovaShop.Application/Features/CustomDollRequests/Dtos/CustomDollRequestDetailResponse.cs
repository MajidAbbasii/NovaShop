namespace NovaShop.Application.Features.CustomDollRequests.Dtos;

public class CustomDollRequestDetailResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerUsername { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BodyColor { get; set; } = string.Empty;
    public string EyeColor { get; set; } = string.Empty;
    public int Height { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? AdminMessage { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
