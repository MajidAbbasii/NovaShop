namespace NovaShop.Application.Messages;

public record StockReservedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public DateTime ReservedUntil { get; init; }
}

public record OrderConfirmedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime ConfirmedAt { get; init; } = DateTime.UtcNow;
}

public record PaymentCompletedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

public record PaymentFailedEvent
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}
