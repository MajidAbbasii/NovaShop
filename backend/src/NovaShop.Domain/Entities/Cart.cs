namespace NovaShop.Domain.Entities;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; init; }
    public User User { get; init; } = null!;

    public List<CartItem> Items { get; private set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.Quantity * i.UnitPrice);

    public void AddItem(Product product, int quantity, int? colorId = null, string colorName = "", decimal? colorPrice = null)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id && i.ProductColorId == colorId);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem
            {
                ProductId = product.Id,
                Product = product,
                ProductColorId = colorId,
                ColorName = colorName,
                Quantity = quantity,
                UnitPrice = colorPrice ?? product.Price
            });
        }
    }
}
