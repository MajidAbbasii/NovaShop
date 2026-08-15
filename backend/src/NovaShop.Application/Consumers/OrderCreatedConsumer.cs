using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Messages;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Consumers;

/// <summary>
/// Handles OrderCreatedEvent — sends the order-placed notification
/// (in-app + SMS) asynchronously via the message bus, decoupling it
/// from the order-creation request path.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        NovaShopDbContext context,
        INotificationService notificationService,
        ILogger<OrderCreatedConsumer> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == context.Message.OrderId, context.CancellationToken);

        if (order == null)
        {
            _logger.LogWarning("OrderCreatedConsumer: order {OrderId} not found", context.Message.OrderId);
            return;
        }

        await _notificationService.NotifyOrderPlacedAsync(order, context.CancellationToken);

        _logger.LogInformation(
            "OrderCreated handled: Order {OrderId} notification dispatched for user {UserId}",
            order.Id, order.UserId);
    }
}

/// <summary>
/// Handles StockReservedEvent — logs the reservation expiry window.
/// Hook point for analytics / cache warming when needed.
/// </summary>
public class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(ILogger<StockReservedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        _logger.LogInformation(
            "Stock reserved for order {OrderId} (user {UserId}) until {ReservedUntil}",
            context.Message.OrderId, context.Message.UserId, context.Message.ReservedUntil);
        return Task.CompletedTask;
    }
}