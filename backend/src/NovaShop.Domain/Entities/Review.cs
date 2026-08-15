namespace NovaShop.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    public int Rating { get; init; } // 1-5
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
