namespace NovaShop.Application.Features.CustomDollRequests.Dtos;

public class CustomDollRequestListResponse
{
    public List<CustomDollRequestDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
