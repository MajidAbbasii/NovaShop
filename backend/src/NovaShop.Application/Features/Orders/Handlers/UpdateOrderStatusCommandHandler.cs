using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Mappers;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        NovaShopDbContext context,
        OrderMapper mapper,
        INotificationService notificationService,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"سفارش {request.OrderId} یافت نشد");

        if (order.Status == request.Status)
            return _mapper.ToDto(order);

        if (!Order.IsValidTransition(order.Status, request.Status))
            throw new InvalidOperationException(
                $"سفارش {order.Id} نمی‌تواند از وضعیت «{order.Status}» به وضعیت «{request.Status}» منتقل شود");

        var changedByUserId = request.ChangedByUserId ?? 0;
        var changedByRole = request.ChangedByRole ?? "Admin";

        // Shipment: assign tracking identifiers when moving to Shipped
        if (request.Status == Order.StatusShipped && string.IsNullOrWhiteSpace(order.TrackingCode))
        {
            order.AssignTrackingCode();
            order.TrackingNumber = GenerateTrackingNumber();
        }

        var history = order.TransitionTo(request.Status, request.Note, changedByUserId, changedByRole);

        // Timestamps (TransitionTo already validated the transition)
        if (request.Status == Order.StatusPaid)
            order.PaidAt = DateTime.UtcNow;
        else if (request.Status == Order.StatusShipped)
            order.ShippedAt = DateTime.UtcNow;

        // Inventory: restore or release based on how far the order progressed
        if (request.Status == Order.StatusCancelled)
        {
            var wasPaid = history.FromStatus is Order.StatusPaid or Order.StatusShipped or Order.StatusDelivered;

            foreach (var item in order.Items)
            {
                var product = item.Product;
                if (product == null) continue;

                int restoreQty = item.Quantity;

                if (wasPaid)
                {
                    // Paid orders already had stock confirmed (permanent deduction).
                    // Restore the sold units back to available stock.
                    product.Stock += restoreQty;
                }
                else if (product.ReservedQuantity > 0)
                {
                    // Pre-paid (Pending/Confirmed): release only this order's reserved
                    // share back to available. Uses the partial-release overload so we
                    // never touch stock reserved by OTHER concurrent orders, and it
                    // is idempotent (clamped to ReservedQuantity, no-op when already 0).
                    restoreQty = Math.Min(product.ReservedQuantity, item.Quantity);
                    product.ReleaseReservation(restoreQty);
                }
                else continue;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    OrderId = order.Id,
                    Type = InventoryTransaction.TypeRelease,
                    Quantity = restoreQty,
                    StockBefore = product.StockBefore,
                    StockAfter = product.StockAfter,
                    Reference = wasPaid
                        ? $"cancelled-paid-order-{order.Id}"
                        : $"cancelled-order-{order.Id}"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // SMS outside the transaction — provider failure must not roll back the status change
        try
        {
            await _notificationService.NotifyOrderStatusChangedAsync(order, request.Status, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status-change SMS for order {OrderId}", order.Id);
        }

        _logger.LogInformation(
            "Order {OrderId} status changed {From} -> {To} by {Role}#{ByUser}",
            order.Id, history.FromStatus, history.ToStatus, changedByRole, changedByUserId);

        var saved = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

        return _mapper.ToDto(saved);
    }

    private static string GenerateTrackingNumber()
    {
        var rand = new Random();
        return string.Concat(Enumerable.Range(0, 12).Select(_ => rand.Next(0, 10).ToString()));
    }
}
