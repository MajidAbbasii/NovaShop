namespace NovaShop.Domain.Entities;

/// <summary>
/// Log entry for every SMS notification sent (or mock-sent) for an order event.
/// </summary>
public class SmsNotification
{
    public const string StatusQueued = "Queued";
    public const string StatusSent = "Sent";
    public const string StatusFailed = "Failed";

    public int Id { get; set; }
    public int? OrderId { get; init; }
    public Order? Order { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty; // OrderPlaced, PaymentSuccessful, ...
    public string Message { get; init; } = string.Empty;
    public string Provider { get; init; } = "Mock";
    public string Status { get; set; } = StatusQueued;
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
