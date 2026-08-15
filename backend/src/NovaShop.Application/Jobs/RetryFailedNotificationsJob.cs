using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Jobs;

/// <summary>
/// Recurring job that re-sends SMS notifications left in a Failed (or Queued) state.
/// Reuses the existing ISmsService — does NOT duplicate message-composition logic.
/// Idempotent: only processes rows still marked Failed/Queued and flips them to
/// Sent/Failed after each attempt, so a retried job never sends the same SMS twice.
/// </summary>
[DisableConcurrentExecution(600)]
public class RetryFailedNotificationsJob
{
    private readonly NovaShopDbContext _context;
    private readonly ISmsService _sms;
    private readonly ILogger<RetryFailedNotificationsJob> _logger;

    public RetryFailedNotificationsJob(
        NovaShopDbContext context,
        ISmsService sms,
        ILogger<RetryFailedNotificationsJob> logger)
    {
        _context = context;
        _sms = sms;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 50;

        var pending = await _context.SmsNotifications
            .Where(n => n.Status == SmsNotification.StatusFailed
                     || n.Status == SmsNotification.StatusQueued)
            .OrderBy(n => n.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            _logger.LogDebug("RetryFailedNotifications: no failed/queued SMS to process.");
            return;
        }

        _logger.LogInformation("RetryFailedNotifications: reprocessing {Count} SMS notification(s).", pending.Count);

        foreach (var note in pending)
        {
            // Re-check status inside the loop — a concurrent run may have changed it.
            if (note.Status != SmsNotification.StatusFailed
                && note.Status != SmsNotification.StatusQueued)
                continue;

            try
            {
                var result = await _sms.SendAsync(
                    new SmsMessage(note.PhoneNumber, note.Message), cancellationToken);

                if (result.Success)
                {
                    note.Status = SmsNotification.StatusSent;
                    note.SentAt = DateTime.UtcNow;
                }
                else
                {
                    note.Status = SmsNotification.StatusFailed;
                }

                note.ProviderMessageId = result.ProviderMessageId;
                note.Error = result.Error;

                _logger.LogInformation(
                    "RetryFailedNotifications: SMS {Id} for order {OrderId} -> {Status}",
                    note.Id, note.OrderId, note.Status);
            }
            catch (Exception ex)
            {
                // Leave as Failed so the next run retries again (bounded by Hangfire retries).
                note.Status = SmsNotification.StatusFailed;
                note.Error = ex.Message;
                _logger.LogError(ex, "RetryFailedNotifications: SMS {Id} send threw.", note.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
