namespace NovaShop.Application.Features.Orders.Dtos;

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? ProductColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
}
