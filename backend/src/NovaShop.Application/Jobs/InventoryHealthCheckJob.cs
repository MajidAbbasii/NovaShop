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
/// Recurring maintenance job that scans product inventory for anomalies:
///  - Negative or corrupt stock (Stock &lt; 0): data corruption — logs at Error level
///    and notifies admins (idempotently, at most once per 24h per product).
///  - Low stock (Stock &lt;= threshold): informational Warning log only.
/// Read-only with respect to stock values — it never deducts or adjusts inventory,
/// so running it repeatedly is always safe (no duplicate business operation).
/// DisableConcurrentExecution prevents two overlapping scans from double-notifying.
/// </summary>
[DisableConcurrentExecution(1800)]
public class InventoryHealthCheckJob
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notifications;
    private readonly JobsOptions _jobs;
    private readonly ILogger<InventoryHealthCheckJob> _logger;

    public InventoryHealthCheckJob(
        NovaShopDbContext context,
        INotificationService notifications,
        IOptions<JobsOptions> jobs,
        ILogger<InventoryHealthCheckJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _jobs = jobs.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var threshold = _jobs.LowStockThreshold;
        var negative = products.Where(p => p.Stock < 0).ToList();
        var low = products.Where(p => p.Stock >= 0 && p.Stock <= threshold).ToList();

        if (negative.Count == 0 && low.Count == 0)
        {
            _logger.LogDebug("InventoryHealthCheck: all {Count} products within healthy bounds.", products.Count);
            return;
        }

        foreach (var p in negative)
        {
            _logger.LogError(
                "InventoryHealthCheck: CORRUPT stock for product {ProductId} ({Name}) = {Stock} (negative). Manual fix required.",
                p.Id, p.Name, p.Stock);

            // Idempotent admin alert: skip if we already raised one in the last 24h.
            var recent = await _context.AppNotifications
                .AsNoTracking()
                .AnyAsync(n => n.Type == "InventoryNegative"
                             && n.OrderId == null
                             && n.CustomDollRequestId == null
                             && n.Title.Contains($"#{p.Id}")
                             && n.CreatedAt > DateTime.UtcNow.AddHours(-24),
                    cancellationToken);

            if (!recent)
            {
                await NotifyAdminsAsync(
                    "InventoryNegative",
                    $"موجودی نامعتبر محصول #{p.Id}",
                    $"محصول «{p.Name}» موجودی منفی دارد ({p.Stock}). نیاز به اصلاح دستی.",
                    cancellationToken);
            }
        }

        foreach (var p in low)
        {
            _logger.LogWarning(
                "InventoryHealthCheck: low stock for product {ProductId} ({Name}) = {Stock} (threshold {Threshold}).",
                p.Id, p.Name, p.Stock, threshold);
        }

        _logger.LogInformation(
            "InventoryHealthCheck: scanned {Total} products — {Negative} negative, {Low} low.",
            products.Count, negative.Count, low.Count);
    }

    private async Task NotifyAdminsAsync(string type, string title, string message, CancellationToken ct)
    {
        var admins = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == User.RoleAdmin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in admins)
        {
            await _notifications.NotifyInAppAsync(adminId, type, title, message, ct: ct);
        }
    }
}
