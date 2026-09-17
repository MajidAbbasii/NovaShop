namespace NovaShop.Application.Features.CustomDollRequests.Dtos;

/// <summary>
/// Extended DTO for admin view: includes customer contact info.
/// Extends CustomDollRequestDto to maintain list compatibility.
/// </summary>
public class AdminCustomDollRequestDto : CustomDollRequestDto
{
    public int UserId { get; set; }
    public string CustomerUsername { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public int? ReviewedBy { get; set; }
}