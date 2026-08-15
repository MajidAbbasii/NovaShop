using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Services;

/// <summary>
/// High-level notification service: composes Persian SMS messages for order
/// events, sends them via the configured provider, and persists a log row.
/// </summary>
public interface INotificationService
{
    Task<SmsNotification?> NotifyOrderPlacedAsync(Order order, CancellationToken ct = default);
    Task<SmsNotification?> NotifyPaymentSuccessfulAsync(Order order, CancellationToken ct = default);
    Task<SmsNotification?> NotifyOrderStatusChangedAsync(Order order, string newStatus, CancellationToken ct = default);
    Task<AppNotification?> NotifyInAppAsync(int userId, string type, string title, string message,
        int? orderId = null, CancellationToken ct = default, int? customDollRequestId = null);
}

public class NotificationService : INotificationService
{
    private readonly NovaShopDbContext _context;
    private readonly ISmsService _sms;
    private readonly IOptions<SmsOptions> _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NovaShopDbContext context,
        ISmsService sms,
        IOptions<SmsOptions> options,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _sms = sms;
        _options = options;
        _logger = logger;
    }

    public async Task<SmsNotification?> NotifyOrderPlacedAsync(Order order, CancellationToken ct = default)
    {
        await NotifyInAppAsync(order.UserId, "OrderPlaced", "سفارش ثبت شد",
            $"سفارش شما با شماره {order.Id} با موفقیت ثبت شد.", order.Id, ct);

        var user = await LoadUserAsync(order.UserId, ct);
        if (!HasPhone(user)) return null;

        var message = BuildMessage(user, order, "order_placed",
            $"سفارش شما با شماره {order.Id} با موفقیت ثبت شد. مبلغ: {order.TotalAmount:N0} تومان. {StoreSuffix()}");

        return await SendAndLogAsync(order.Id, user.PhoneNumber, "OrderPlaced", message, ct);
    }

    public async Task<SmsNotification?> NotifyPaymentSuccessfulAsync(Order order, CancellationToken ct = default)
    {
        await NotifyInAppAsync(order.UserId, "PaymentSuccessful", "پرداخت موفق",
            $"پرداخت سفارش {order.Id} با موفقیت انجام شد. مبلغ: {order.TotalAmount:N0} تومان.", order.Id, ct);

        var user = await LoadUserAsync(order.UserId, ct);
        if (!HasPhone(user)) return null;

        var message = BuildMessage(user, order, "payment_successful",
            $"پرداخت سفارش {order.Id} با موفقیت انجام شد. مبلغ: {order.TotalAmount:N0} تومان. {StoreSuffix()}");

        return await SendAndLogAsync(order.Id, user.PhoneNumber, "PaymentSuccessful", message, ct);
    }

    public async Task<SmsNotification?> NotifyOrderStatusChangedAsync(Order order, string newStatus, CancellationToken ct = default)
    {
        var (title, body) = StatusTitleText(newStatus, order);
        await NotifyInAppAsync(order.UserId, $"Status_{newStatus}", title, body, order.Id, ct);

        var user = await LoadUserAsync(order.UserId, ct);
        if (!HasPhone(user)) return null;

        var trackingPart = !string.IsNullOrWhiteSpace(order.TrackingCode)
            ? $"\nکد رهگیری: {order.TrackingCode}"
            : string.Empty;

        var message = BuildMessage(user, order, "order_status",
            $"{body}{trackingPart}\n{StoreSuffix()}");

        return await SendAndLogAsync(order.Id, user.PhoneNumber, $"Status_{newStatus}", message, ct);
    }

    public async Task<AppNotification?> NotifyInAppAsync(int userId, string type, string title, string message,
        int? orderId = null, CancellationToken ct = default, int? customDollRequestId = null)
    {
        try
        {
            var notification = new AppNotification
            {
                UserId = userId,
                OrderId = orderId,
                CustomDollRequestId = customDollRequestId,
                Type = type,
                Channel = AppNotification.ChannelInApp,
                Title = title,
                Message = message,
                Status = AppNotification.StatusSent,
                SentAt = DateTime.UtcNow
            };
            _context.AppNotifications.Add(notification);
            await _context.SaveChangesAsync(ct);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create in-app notification userId={UserId} type={Type}", userId, type);
            return null;
        }
    }

    private static (string Title, string Body) StatusTitleText(string newStatus, Order order) => newStatus switch
    {
        Order.StatusPaid => ("پرداخت موفق", $"پرداخت سفارش {order.Id} با موفقیت انجام شد."),
        Order.StatusProcessing => ("در حال آماده‌سازی", $"سفارش {order.Id} در حال آماده‌سازی است."),
        Order.StatusReadyForPickup => ("آماده دریافت حضوری", $"سفارش {order.Id} آماده دریافت حضوری است."),
        Order.StatusShipped => ("ارسال شد", $"سفارش {order.Id} ارسال شد."),
        Order.StatusDelivered => ("تحویل شد", $"سفارش {order.Id} با موفقیت تحویل داده شد."),
        Order.StatusCancelled => ("لغو شد", $"سفارش {order.Id} لغو شد."),
        Order.StatusRefunded => ("بازگشت وجه", $"مبلغ سفارش {order.Id} به کیف پول شما بازگردانده شد."),
        _ => ($"وضعیت: {newStatus}", $"وضعیت سفارش {order.Id} به «{newStatus}» تغییر کرد.")
    };

    private async Task<SmsNotification?> SendAndLogAsync(
        int? orderId, string phone, string eventType, string message, CancellationToken ct)
    {
        SmsSendResult result;
        try
        {
            result = await _sms.SendAsync(new SmsMessage(phone, message), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed for order {OrderId}, event {Event}", orderId, eventType);
            result = new SmsSendResult(false, Error: ex.Message);
        }

        var notification = new SmsNotification
        {
            OrderId = orderId,
            PhoneNumber = phone,
            EventType = eventType,
            Message = message,
            Provider = _sms.ProviderName,
            Status = result.Success ? SmsNotification.StatusSent : SmsNotification.StatusFailed,
            ProviderMessageId = result.ProviderMessageId,
            Error = result.Error,
            SentAt = result.Success ? DateTime.UtcNow : null
        };

        _context.SmsNotifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        return notification;
    }

    private string BuildMessage(User user, Order order, string template, string body)
    {
        // Template placeholder substitution (template ID sent to provider when configured).
        return body;
    }
    private static string StatusFa(string status) => status switch
    {
        Order.StatusPending => "در انتظار پرداخت",
        Order.StatusConfirmed => "تأیید شده",
        Order.StatusProcessing => "در حال آماده‌سازی",
        Order.StatusPaid => "پرداخت موفق",
        Order.StatusShipped => "ارسال شده",
        Order.StatusDelivered => "تحویل شده",
        Order.StatusCancelled => "لغو شده",
        Order.StatusFailed => "ناموفق",
        _ => status
    };

    private string StoreSuffix() => $"{_options.Value.StoreName}";

    private static bool HasPhone(User? user)
        => user != null && !string.IsNullOrWhiteSpace(user.PhoneNumber);

    private async Task<User?> LoadUserAsync(int userId, CancellationToken ct)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
}
