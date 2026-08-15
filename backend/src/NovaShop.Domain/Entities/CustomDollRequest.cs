namespace NovaShop.Domain.Entities;

/// <summary>
/// A customer-uploaded photo request for a custom crocheted doll.
/// Admin reviews and sets the price; customer is notified.
/// </summary>
public class CustomDollRequest
{
    public const string StatusPendingReview = "PendingReview";
    public const string StatusApproved = "Approved";
    public const string StatusRejected = "Rejected";
    public const string StatusCustomerAccepted = "CustomerAccepted";
    public const string CurrencyToman = "Toman";

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; init; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = StatusPendingReview;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = CurrencyToman;
    public string? AdminMessage { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Set when an automated reminder for this still-pending request has been sent to
    /// admins. Used by the CustomDollRequestReminderJob to avoid repeated reminders.
    /// </summary>
    public DateTime? ReminderSentAt { get; set; }
}