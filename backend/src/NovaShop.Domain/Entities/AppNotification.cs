namespace NovaShop.Domain.Entities;

/// <summary>
/// In-app notification for the customer (notification center).
/// Complement to SmsNotification (external channel) — both are produced by
/// the centralized notification service on order events.
/// </summary>
public class AppNotification
{
    public const string ChannelInApp = "InApp";
    public const string ChannelSms = "Sms";
    public const string ChannelEmail = "Email";

    public const string StatusPending = "Pending";
    public const string StatusSent = "Sent";
    public const string StatusFailed = "Failed";

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; init; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? CustomDollRequestId { get; set; }
    public CustomDollRequest? CustomDollRequest { get; set; }
    public string Type { get; set; } = string.Empty; // OrderPlaced, PaymentSuccessful, OrderShipped, ...
    public string Channel { get; set; } = ChannelInApp;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = StatusSent;
    public bool IsRead { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
