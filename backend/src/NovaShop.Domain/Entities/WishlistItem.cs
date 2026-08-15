namespace NovaShop.Domain.Entities;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;
    public DateTime AddedAt { get; init; } = DateTime.UtcNow;
    public string? Note { get; init; }
}
