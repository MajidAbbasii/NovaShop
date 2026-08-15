namespace NovaShop.Application.Messages;

public record ProductCreatedEvent
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
