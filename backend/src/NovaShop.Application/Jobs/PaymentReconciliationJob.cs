using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Jobs;

/// <summary>
/// Reconciles pending payments against the (future) payment gateway.
///
/// Online payments are currently DISABLED (PaymentPolicy.OnlinePaymentEnabled = false),
/// so this job is a guarded no-op that logs and exits. The reconciliation logic is
/// intentionally left as a single extension point: when online payment is enabled,
/// set PaymentMethod/Status filters here and compare against the gateway ledger.
///
/// This job does NOT enable online payments and never mutates orders while disabled.
/// </summary>
public class PaymentReconciliationJob
{
    private readonly NovaShopDbContext _context;
    private readonly ILogger<PaymentReconciliationJob> _logger;

    public PaymentReconciliationJob(
        NovaShopDbContext context,
        ILogger<PaymentReconciliationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!PaymentPolicy.OnlinePaymentEnabled)
        {
            _logger.LogDebug(
                "PaymentReconciliation: skipped — online payments are disabled (PaymentPolicy.OnlinePaymentEnabled=false).");
            return;
        }

        // --- Extension point for when online payments are enabled ---
        // Find orders with PaymentMethod != "InPerson" and Payment.Status == "Pending",
        // then match against the gateway transaction store and update statuses.
        // Left intentionally unimplemented until the payment gateway is wired up.
        var pendingPayments = await _context.Payments
            .Where(p => p.PaymentMethod != "InPerson" && p.Status == "Pending")
            .Include(p => p.Order)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "PaymentReconciliation: online payments enabled — {Count} pending payment(s) found for reconciliation.",
            pendingPayments.Count);

        // TODO: integrate with real payment gateway reconciliation API.
    }
}
