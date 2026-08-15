namespace NovaShop.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; init; }
    public Cart Cart { get; init; } = null!;
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;
    public int? ProductColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; init; }
}
