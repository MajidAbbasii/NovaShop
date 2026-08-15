using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Jobs;

/// <summary>
/// Recurring job that reminds administrators about custom-doll requests left in
/// PendingReview for too long. Idempotent: a request is reminded at most once via the
/// ReminderSentAt marker, so a repeat run never spams admins and never changes
/// business status. DisableConcurrentExecution avoids overlapping scans.
/// </summary>
[DisableConcurrentExecution(3600)]
public class CustomDollRequestReminderJob
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notifications;
    private readonly JobsOptions _jobs;
    private readonly ILogger<CustomDollRequestReminderJob> _logger;

    public CustomDollRequestReminderJob(
        NovaShopDbContext context,
        INotificationService notifications,
        IOptions<JobsOptions> jobs,
        ILogger<CustomDollRequestReminderJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _jobs = jobs.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_jobs.CustomDollReminderAfterDays);

        var stale = await _context.CustomDollRequests
            .Where(r => r.Status == CustomDollRequest.StatusPendingReview
                     && r.CreatedAt <= cutoff
                     && r.ReminderSentAt == null)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            _logger.LogDebug("CustomDollRequestReminder: no aged pending requests to remind.");
            return;
        }

        _logger.LogInformation("CustomDollRequestReminder: {Count} aged pending request(s) to remind.", stale.Count);

        var adminIds = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == User.RoleAdmin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            _logger.LogWarning("CustomDollRequestReminder: no active admin users found to notify.");
        }

        foreach (var req in stale)
        {
            foreach (var adminId in adminIds)
            {
                await _notifications.NotifyInAppAsync(
                    adminId,
                    "CustomDollReminder",
                    "یادآوری بررسی درخواست عروسک سفارشی",
                    $"درخواست عروسک سفارشی #{req.Id} از تاریخ {req.CreatedAt:yyyy-MM-dd} در انتظار بررسی است.",
                    customDollRequestId: req.Id,
                    ct: cancellationToken);
            }

            req.ReminderSentAt = DateTime.UtcNow;
            _logger.LogInformation("CustomDollRequestReminder: reminded admins about request #{Id}.", req.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
