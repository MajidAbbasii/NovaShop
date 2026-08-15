namespace NovaShop.Application.Messages;

public record OrderCreatedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
