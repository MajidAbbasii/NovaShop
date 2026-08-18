using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Mappers;
using NovaShop.Application.Messages;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Common.Models;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

public class CreateOrderFromCartCommandHandler : IRequestHandler<CreateOrderFromCartCommand, OrderDto>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _orderMapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IReservationScheduler _reservationScheduler;
    private readonly INotificationService _notificationService;
    private readonly IDiscountRepository _discountRepository;
    private readonly IShippingCostService _shippingCostService;
    private readonly ILogger<CreateOrderFromCartCommandHandler> _logger;

    private static readonly TimeSpan ReservationTimeout = TimeSpan.FromMinutes(15);

    public CreateOrderFromCartCommandHandler(
        NovaShopDbContext context,
        OrderMapper orderMapper,
        IPublishEndpoint publishEndpoint,
        IReservationScheduler reservationScheduler,
        INotificationService notificationService,
        IDiscountRepository discountRepository,
        IShippingCostService shippingCostService,
        ILogger<CreateOrderFromCartCommandHandler> logger)
    {
        _context = context;
        _orderMapper = orderMapper;
        _publishEndpoint = publishEndpoint;
        _reservationScheduler = reservationScheduler;
        _notificationService = notificationService;
        _discountRepository = discountRepository;
        _shippingCostService = shippingCostService;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderFromCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Idempotency check
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _context.Orders
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existing != null)
            {
                _logger.LogInformation("Idempotency hit for key {Key}, returning existing order {OrderId}",
                    request.IdempotencyKey, existing.Id);

                return _orderMapper.ToDto(await _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .Include(o => o.Payment)
                    .FirstAsync(o => o.Id == existing.Id, cancellationToken));
            }
        }

        // 2. Load cart
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Colors)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null || cart.Items.Count == 0)
        {
            _logger.LogWarning("Cart is empty or not found for user {UserId}", request.UserId);
            throw new InvalidOperationException("سبد خرید خالی است. لطفاً ابتدا محصولی اضافه کنید.");
        }

        // Persist contact phone on the user so order SMS notifications can be sent.
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user != null && string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber.Trim();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // 3. Reservation phase — begin transaction
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var reservationExpiresAt = DateTime.UtcNow.Add(ReservationTimeout);

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;
                product.ReserveStock(cartItem.Quantity, reservationExpiresAt);

                // Variant products: also reserve the chosen color's stock so a
                // specific color cannot be oversold independently of product stock.
                if (cartItem.ProductColorId.HasValue)
                {
                    var color = product.Colors.FirstOrDefault(c => c.Id == cartItem.ProductColorId.Value);
                    if (color == null)
                        throw new InvalidOperationException($"رنگ انتخاب شده برای محصول {product.Name} یافت نشد");
                    color.ReserveStock(cartItem.Quantity);
                }
            }

            // Temporary business mode: online payment disabled → always CashOnDelivery
            // (پرداخت هنگام تحویل). When PaymentPolicy:OnlinePaymentEnabled=true this
            // is a no-op (request method passes through, after normalization).
            var paymentMethod = !PaymentPolicy.OnlinePaymentEnabled
                ? Order.PaymentMethodCashOnDelivery
                : CreateOrderFromCartCommandValidator.NormalizePaymentMethod(request.PaymentMethod);

            // Subtotal is derived from the trusted cart (DB-backed unit prices).
            var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

            var order = new Order
            {
                UserId = request.UserId,
                TotalAmount = subtotal,
                OriginalTotal = subtotal,
                ShippingAddress = request.ShippingAddress,
                ShippingMethod = request.ShippingMethod,
                ShippingCost = 0m,
                PickupLocation = request.PickupLocation,
                PickupInstructions = request.PickupInstructions,
                PaymentMethod = paymentMethod,
                PaymentStatus = Order.PaymentPending,
                Status = Order.StatusPending,
                IdempotencyKey = request.IdempotencyKey ?? string.Empty,
                ReservationExpiresAt = reservationExpiresAt
            };

            foreach (var cartItem in cart.Items)
            {
                order.AddItem(new OrderItem
                                {
                                    ProductId = cartItem.ProductId,
                                    ProductColorId = cartItem.ProductColorId,
                                    ColorName = cartItem.ColorName,
                                    Quantity = cartItem.Quantity,
                                    UnitPrice = cartItem.UnitPrice
                                });
            }

            // Apply discount code (optional) — validate + compute before persisting
            if (!string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                var discount = await _discountRepository.GetByCodeIgnoringCaseAsync(request.DiscountCode.Trim());
                if (discount == null)
                    throw new InvalidOperationException("کد تخفیف معتبر نیست");
                if (!discount.IsValid(DateTime.UtcNow))
                    throw new InvalidOperationException("کد تخفیف منقضی شده یا غیرفعال است");
                var orderTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
                if (orderTotal < discount.MinOrderAmount)
                    throw new InvalidOperationException(
                        $"حداقل مبلغ سفارش برای این تخفیف {discount.MinOrderAmount:N0} تومان است");
                order.ApplyDiscount(discount, orderTotal);
                discount.IncrementUsage();
            }

            // Shipping is added on top of item totals. It is recalculated HERE (after any
            // discount) from the server-side service using the trusted pre-discount subtotal —
            // the free-shipping threshold is evaluated on that subtotal (existing business rule).
            // The client never supplies the cost.
            var itemsSubtotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
            var shipping = _shippingCostService.Calculate(itemsSubtotal, request.ShippingMethod);
            order.ShippingMethod = shipping.ShippingMethod;
            order.ShippingCost = shipping.ShippingCost;
            order.OriginalTotal += shipping.ShippingCost;
            order.TotalAmount += shipping.ShippingCost;

            var payment = new Payment
            {
                Order = order,
                Amount = order.TotalAmount,
                PaymentMethod = paymentMethod,
                Status = "Pending",
                TransactionId = string.Empty,
                IdempotencyKey = string.Empty
            };

            // Initial status history entry (Pending)
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Order = order,
                FromStatus = string.Empty,
                ToStatus = Order.StatusPending,
                Note = "سفارش ثبت شد",
                ChangedByUserId = request.UserId,
                ChangedByRole = "Customer",
                ChangedAt = DateTime.UtcNow
            });

            // Inventory ledger: reserve entries
            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    Order = order,
                    Type = InventoryTransaction.TypeReserve,
                    Quantity = cartItem.Quantity,
                    StockBefore = product.StockBefore,
                    StockAfter = product.StockAfter,
                    Reference = $"order-{order.Id}"
                });
            }

            _context.Orders.Add(order);
            _context.Payments.Add(payment);
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Assign tracking code (needs order Id)
            order.AssignTrackingCode();
            _context.Orders.Update(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} created. Stock reserved for user {UserId}. Expires at {ExpiresAt}",
                order.Id, request.UserId, reservationExpiresAt);

            // Schedule expiry via Hangfire (through abstraction)
            _reservationScheduler.ScheduleExpiry(order.Id, ReservationTimeout);

            // Publish events
            await _publishEndpoint.Publish(new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = request.UserId,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            }, cancellationToken);

            await _publishEndpoint.Publish(new StockReservedEvent
            {
                OrderId = order.Id,
                UserId = request.UserId,
                ReservedUntil = reservationExpiresAt
            }, cancellationToken);

            // Send SMS notification (async, non-blocking)
            try
            {
                await _notificationService.NotifyOrderPlacedAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order-placed SMS for order {OrderId}", order.Id);
            }

            var savedOrder = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Payment)
                .FirstAsync(o => o.Id == order.Id, cancellationToken);

            return _orderMapper.ToDto(savedOrder);
        }
        catch
        {
            _logger.LogError("Order creation failed for user {UserId}. Transaction rolled back.", request.UserId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
