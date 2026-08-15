using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Jobs;

public class ReleaseExpiredReservationsJob
{
    private readonly NovaShopDbContext _context;
    private readonly ILogger<ReleaseExpiredReservationsJob> _logger;

    public ReleaseExpiredReservationsJob(
        NovaShopDbContext context,
        ILogger<ReleaseExpiredReservationsJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Releases expired reservations for ALL orders past their expiry.
    /// Called via Hangfire recurring job every 5 minutes.
    /// </summary>
    public async Task ReleaseAllExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredOrders = await _context.Orders
            .Where(o => o.Status == Order.StatusPending
                     && o.ReservationExpiresAt != null
                     && o.ReservationExpiresAt <= now)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
            return;

        _logger.LogInformation("Found {Count} expired reservations to release", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            try
            {
                foreach (var item in order.Items)
                {
                    if (item.Product == null) continue;
                    item.Product.ReleaseReservation();

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        OrderId = order.Id,
                        Type = InventoryTransaction.TypeRelease,
                        Quantity = item.Quantity,
                        StockBefore = item.Product.StockBefore,
                        StockAfter = item.Product.StockAfter,
                        Reference = $"expired-order-{order.Id}"
                    });
                }

                order.TransitionTo(Order.StatusFailed, "انقضای رزرو موجودی", 0, "System");

                if (order.Payment != null)
                {
                    order.Payment.Status = "Failed";
                }

                _logger.LogInformation("Released expired reservation for order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release reservation for order {OrderId}", order.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Releases reserved stock for a specific order (called via Hangfire scheduled job on timeout).
    /// This is a safety net — the recurring job also handles this.
    /// </summary>
    public async Task ReleaseAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("ReleaseReservation: Order {OrderId} not found", orderId);
            return;
        }

        // Only release if order is still Pending (not Confirmed/Paid)
        if (order.Status != Order.StatusPending)
        {
            _logger.LogInformation(
                "ReleaseReservation: Order {OrderId} is {Status}, no release needed",
                orderId, order.Status);
            return;
        }

        var orderWithItems = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == orderId, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var item in orderWithItems.Items)
            {
                if (item.Product == null) continue;
                item.Product.ReleaseReservation();

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    OrderId = orderId,
                    Type = InventoryTransaction.TypeRelease,
                    Quantity = item.Quantity,
                    StockBefore = item.Product.StockBefore,
                    StockAfter = item.Product.StockAfter,
                    Reference = $"expired-order-{orderId}"
                });
            }

            orderWithItems.TransitionTo(Order.StatusFailed, "انقضای رزرو موجودی", 0, "System");

            if (orderWithItems.Payment != null)
            {
                orderWithItems.Payment.Status = "Failed";
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Released expired reservation for order {OrderId}. {ItemCount} items restored.",
                orderId, orderWithItems.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release reservation for order {OrderId}", orderId);
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}
