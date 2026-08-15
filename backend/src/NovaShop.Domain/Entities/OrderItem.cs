namespace NovaShop.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; init; }
    public Order Order { get; init; } = null!;
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;
    public int? ProductColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
