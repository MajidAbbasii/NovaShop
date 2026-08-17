using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Handlers;
using NovaShop.Application.Jobs;
using NovaShop.Application.Mappers;
using NovaShop.Application.Messages;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;
using Xunit;

namespace NovaShop.Tests.Application;

public class CheckoutFlowIntegrationTests
{
    private static DbContextOptions<NovaShopDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<NovaShopDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    /// <summary>Seed data, save, return context. Order.Id is assigned after seed.</summary>
    private static async Task<NovaShopDbContext> SeedAsync(string dbName, Action<NovaShopDbContext> seed)
    {
        var ctx = new NovaShopDbContext(CreateOptions(dbName));
        seed(ctx);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // add payment to an existing order after seeding — EF populates navigation via FK
    private static async Task AddPaymentAsync(NovaShopDbContext ctx, Order order, string method, decimal amount, string status, string? txnId = null)
    {
        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = method,
            Amount = amount,
            Status = status,
            TransactionId = txnId ?? string.Empty
        };
        ctx.Payments.Add(payment);
        await ctx.SaveChangesAsync();
    }

    // ================================================================
    // CREATE ORDER FROM CART
    // ================================================================

    [Fact]
    public async Task CreateOrderFromCart_HappyPath_ReservesStockAndCreatesOrder()
    {
        var dbName = $"{nameof(CreateOrderFromCart_HappyPath_ReservesStockAndCreatesOrder)}_{Guid.NewGuid():N}";
        var userId = 1;
        var pA = new Product { Id = 1, Name = "A", Price = 10m, Stock = 10, CategoryId = 1 };
        var pB = new Product { Id = 2, Name = "B", Price = 20m, Stock = 5, CategoryId = 1 };

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.AddRange(pA, pB);
            var cart = new Cart { UserId = userId };
            cart.AddItem(pA, 2);
            cart.AddItem(pB, 1);
            db.Carts.Add(cart);
        });

        var publish = new Mock<IPublishEndpoint>();
        var scheduler = new Mock<IReservationScheduler>();
        var notification = new Mock<INotificationService>();

        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), publish.Object, scheduler.Object, notification.Object,
            new Mock<IDiscountRepository>().Object,
            new ShippingCostService(),
            Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var result = await handler.Handle(
            new CreateOrderFromCartCommand(userId, "123 Main St", "InPerson"), default);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(Order.StatusPending, result.Status);
        Assert.Equal(2, result.Items.Count);
        // Subtotal = 2*10 + 1*20 = 40. POST (< free-shipping threshold) => shipping 59,900.
        Assert.Equal(40m, result.OriginalTotal - result.ShippingCost);
        Assert.Equal(59_900m, result.ShippingCost);
        Assert.Equal(59_940m, result.TotalAmount);
        Assert.Equal("123 Main St", result.ShippingAddress);

        // stock reserved: 10-2=8, 5-1=4
        Assert.Equal(8, (await ctx.Products.FindAsync(1))!.Stock);
        Assert.Equal(2, (await ctx.Products.FindAsync(1))!.ReservedQuantity);
        Assert.Equal(4, (await ctx.Products.FindAsync(2))!.Stock);
        Assert.Equal(1, (await ctx.Products.FindAsync(2))!.ReservedQuantity);

        Assert.Null(await ctx.Carts.FirstOrDefaultAsync(c => c.UserId == userId));

        publish.Verify(p => p.Publish(It.IsAny<OrderCreatedEvent>(), default), Times.Once);
        publish.Verify(p => p.Publish(It.IsAny<StockReservedEvent>(), default), Times.Once);
        scheduler.Verify(s => s.ScheduleExpiry(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCart_EmptyCart_Throws()
    {
        var dbName = $"empty_{Guid.NewGuid():N}";
        var ctx = await SeedAsync(dbName, _ => { });

        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), new Mock<IDiscountRepository>().Object,
            new ShippingCostService(),
            Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateOrderFromCartCommand(1, "Addr", "InPerson"), default));
        Assert.Contains("سبد خرید خالی", ex.Message);
    }

    [Fact]
    public async Task CreateOrderFromCart_InsufficientStock_Throws()
    {
        var dbName = $"nostock_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "Low", Price = 5m, Stock = 1, CategoryId = 1 };

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            var cart = new Cart { UserId = 1 };
            cart.AddItem(p, 10);
            db.Carts.Add(cart);
        });

        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), new Mock<IDiscountRepository>().Object,
            new ShippingCostService(),
            Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateOrderFromCartCommand(1, "Addr", "InPerson"), default));
        Assert.Contains("موجودی", ex.Message);
    }

    [Fact]
    public async Task CreateOrderFromCart_IdempotencyKey_ReturnsExistingOrder()
    {
        var dbName = $"idemp_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "Prod", Price = 10m, Stock = 10, CategoryId = 1 };

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            var cart = new Cart { UserId = 1 };
            cart.AddItem(p, 1);
            db.Carts.Add(cart);
        });

        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), new Mock<IDiscountRepository>().Object,
            new ShippingCostService(),
            Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var cmd = new CreateOrderFromCartCommand(1, "Addr", "InPerson", IdempotencyKey: "dup-123");
        var r1 = await handler.Handle(cmd, default);
        var r2 = await handler.Handle(cmd, default);

        Assert.Equal(r1.Id, r2.Id);
    }

    // ================================================================
    // PROCESS PAYMENT
    // ================================================================

    [Fact]
    public async Task ProcessPayment_Success_ConfirmsOrderAndFinalizesStock()
    {
        var dbName = $"pay_ok_{Guid.NewGuid():N}";
        var userId = 1;
        var prev = PaymentPolicy.OnlinePaymentEnabled;
        PaymentPolicy.OnlinePaymentEnabled = true; // gateway test — online allowed
        try
        {
            var product = new Product { Id = 1, Name = "W", Price = 50m, Stock = 8, ReservedQuantity = 2, CategoryId = 1 };
            var order = new Order { UserId = userId, TotalAmount = 100m, Status = Order.StatusPending, ShippingAddress = "A" };
            order.AddItem(new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 50m, Product = product });
            // ... seed products + orders
            var ctx = await SeedAsync(dbName, db =>
            {
                db.Products.Add(product);
                db.Orders.Add(order);
            });
            // AddPaymentAsync must be called after the order has an Id assigned by the DB
            await AddPaymentAsync(ctx, order, "CreditCard", 100m, "Pending");

            var gateway = new Mock<IPaymentGateway>();
            gateway.Setup(g => g.InitiatePaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "TXN-OK", Authority = "AUTH-OK", RedirectUrl = "http://gw/ok" });
            var publish = new Mock<IPublishEndpoint>();

            var handler = new ProcessPaymentCommandHandler(
                ctx, gateway.Object, new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), publish.Object, Mock.Of<INotificationService>(),
                Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

            var result = await handler.Handle(new ProcessPaymentCommand(order.Id, userId), default);

            Assert.True(result.Success);
            Assert.Equal("Pending", result.PaymentStatus);

            var saved = await ctx.Orders.Include(o => o.Items!).ThenInclude(i => i.Product).FirstAsync(o => o.Id == order.Id);
            Assert.Equal(Order.StatusPending, saved.Status);
            Assert.Equal("Pending", saved.Payment!.Status);
            Assert.Equal(8, saved.Items[0].Product!.Stock);
            Assert.Equal(2, saved.Items[0].Product!.ReservedQuantity);

            publish.Verify(p => p.Publish(It.IsAny<OrderConfirmedEvent>(), default), Times.Never);
            publish.Verify(p => p.Publish(It.IsAny<PaymentCompletedEvent>(), default), Times.Never);
        }
        finally { PaymentPolicy.OnlinePaymentEnabled = prev; }
    }

    [Fact]
    public async Task ProcessPayment_Failure_ReleasesStockAndFailsOrder()
    {
        var dbName = $"pay_fail_{Guid.NewGuid():N}";
        var userId = 1;
        var prev = PaymentPolicy.OnlinePaymentEnabled;
        PaymentPolicy.OnlinePaymentEnabled = true; // gateway test — online allowed
        try
        {
            var product = new Product { Id = 1, Name = "W", Price = 50m, Stock = 8, ReservedQuantity = 2, CategoryId = 1 };
            var order = new Order { UserId = userId, TotalAmount = 100m, Status = Order.StatusPending };
            order.AddItem(new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 50m, Product = product });

            var ctx = await SeedAsync(dbName, db =>
            {
                db.Products.Add(product);
                db.Orders.Add(order);
            });
            await AddPaymentAsync(ctx, order, "CreditCard", 100m, "Pending");

            var gateway = new Mock<IPaymentGateway>();
            gateway.Setup(g => g.InitiatePaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(new PaymentResult { Success = false, FailureReason = "Card declined" });

            var handler = new ProcessPaymentCommandHandler(
                ctx, gateway.Object, new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), Mock.Of<IPublishEndpoint>(), Mock.Of<INotificationService>(),
                Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

            var result = await handler.Handle(new ProcessPaymentCommand(order.Id, userId), default);

            Assert.False(result.Success);
            Assert.Equal("Failed", result.PaymentStatus);
            // FIXED: payment initiation failure now releases reserved stock
            // immediately (previously the expired-reservation Hangfire job was the
            // only safety net, leaving stock locked for up to 15 minutes).
            Assert.Equal(10, product.Stock);
            Assert.Equal(0, product.ReservedQuantity);
            Assert.Equal(Order.StatusFailed, (await ctx.Orders.FindAsync(order.Id))!.Status);
        }
        finally { PaymentPolicy.OnlinePaymentEnabled = prev; }
    }

    [Fact]
    public async Task ProcessPayment_IdempotencyKey_ReturnsCachedResult()
    {
        var dbName = $"pay_id_{Guid.NewGuid():N}";
        var userId = 1;
        var prev = PaymentPolicy.OnlinePaymentEnabled;
        PaymentPolicy.OnlinePaymentEnabled = true; // gateway test — online allowed
        try
        {
            var product = new Product { Id = 1, Name = "W", Price = 10m, Stock = 10, CategoryId = 1 };
            var order = new Order { UserId = userId, TotalAmount = 10m, Status = Order.StatusPending };
            order.AddItem(new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 10m, Product = product });

            var ctx = await SeedAsync(dbName, db =>
            {
                db.Products.Add(product);
                db.Orders.Add(order);
            });
            await AddPaymentAsync(ctx, order, "CreditCard", 10m, "Pending");

            var gateway = new Mock<IPaymentGateway>();
            gateway.Setup(g => g.InitiatePaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "TXN-ABC", Authority = "AUTH-ABC", RedirectUrl = "http://gw/abc" });

            var handler = new ProcessPaymentCommandHandler(
                ctx, gateway.Object, new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), Mock.Of<IPublishEndpoint>(), Mock.Of<INotificationService>(),
                Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

            var cmd = new ProcessPaymentCommand(order.Id, userId, IdempotencyKey: "idem-1");
            var r1 = await handler.Handle(cmd, default);

            // handler persists gateway Authority as the payment's IdempotencyKey,
            // so a retry with that authority must not re-initiate the gateway
            var retry = new ProcessPaymentCommand(order.Id, userId, IdempotencyKey: r1.Authority!);
            var r2 = await handler.Handle(retry, default);

            Assert.True(r1.Success);
            Assert.True(r2.Success);
            // second call returns the persisted gateway transaction id, no re-initiation
            Assert.Equal("TXN-ABC", r2.TransactionId);
            Assert.Equal(r1.Authority, r2.Authority);
            gateway.Verify(g => g.InitiatePaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        }
        finally { PaymentPolicy.OnlinePaymentEnabled = prev; }
    }

    [Fact]
    public async Task ProcessPayment_InPersonOrder_WhenOnlineDisabled_Throws()
    {
        var dbName = $"pay_inperson_{Guid.NewGuid():N}";
        var userId = 1;
        var prev = PaymentPolicy.OnlinePaymentEnabled;
        PaymentPolicy.OnlinePaymentEnabled = false; // business mode default
        try
        {
            var order = new Order { UserId = userId, TotalAmount = 100m, Status = Order.StatusPending, PaymentMethod = "InPerson" };
            var ctx = await SeedAsync(dbName, db => db.Orders.Add(order));
            await AddPaymentAsync(ctx, order, "InPerson", 100m, "Pending");

            var handler = new ProcessPaymentCommandHandler(
                ctx, Mock.Of<IPaymentGateway>(), new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), Mock.Of<IPublishEndpoint>(),
                Mock.Of<INotificationService>(), Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(new ProcessPaymentCommand(order.Id, userId), default));
            Assert.Contains("غیرفعال", ex.Message);
        }
        finally { PaymentPolicy.OnlinePaymentEnabled = prev; }
    }

    [Fact]
    public async Task ProcessPayment_UnauthorizedUser_Throws()
    {
        var dbName = $"pay_unauth_{Guid.NewGuid():N}";
        var order = new Order { Id = 42, UserId = 2, TotalAmount = 100m, Status = Order.StatusPending };
        var ctx = await SeedAsync(dbName, db => db.Orders.Add(order));
        await AddPaymentAsync(ctx, order, "CreditCard", 100m, "Pending");

        var handler = new ProcessPaymentCommandHandler(
            ctx, Mock.Of<IPaymentGateway>(), new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), Mock.Of<IPublishEndpoint>(),
            Mock.Of<INotificationService>(), Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new ProcessPaymentCommand(order.Id, 1), default));
        Assert.Contains("دسترسی", ex.Message);
    }

    [Fact]
    public async Task ProcessPayment_AlreadyPaid_ReturnsIdempotentResult()
    {
        var dbName = $"paid_{Guid.NewGuid():N}";
        var userId = 1;
        var order = new Order { UserId = userId, TotalAmount = 100m, Status = Order.StatusPaid, PaymentMethod = "Wallet" };
        var ctx = await SeedAsync(dbName, db => db.Orders.Add(order));
        await AddPaymentAsync(ctx, order, "Wallet", 100m, "Completed", "TXN-OLD");

        var handler = new ProcessPaymentCommandHandler(
            ctx, Mock.Of<IPaymentGateway>(), new WalletService(ctx, Mock.Of<ILogger<WalletService>>()), Mock.Of<IPublishEndpoint>(),
            Mock.Of<INotificationService>(), Mock.Of<ILogger<ProcessPaymentCommandHandler>>());

        var result = await handler.Handle(new ProcessPaymentCommand(order.Id, userId), default);
        Assert.True(result.Success);
        Assert.Equal("TXN-OLD", result.TransactionId);
    }

    // ================================================================
    // EXPIRED RESERVATION CLEANUP
    // ================================================================

    [Fact]
    public async Task ReleaseExpiredReservations_ReleasesStockAndFailsOrders()
    {
        var dbName = $"release_{Guid.NewGuid():N}";

        var product = new Product { Id = 1, Name = "Expirable", Price = 10m, Stock = 5, ReservedQuantity = 3, CategoryId = 1 };
        var expired = new Order { UserId = 1, TotalAmount = 30m, Status = Order.StatusPending, ReservationExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        expired.AddItem(new OrderItem { ProductId = 1, Quantity = 3, UnitPrice = 10m, Product = product });

        var freshP = new Product { Id = 2, Name = "Fresh", Price = 5m, Stock = 10, CategoryId = 1 };
        var fresh = new Order { UserId = 1, TotalAmount = 5m, Status = Order.StatusPending, ReservationExpiresAt = DateTime.UtcNow.AddHours(1) };
        fresh.AddItem(new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 5m, Product = freshP });

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.AddRange(product, freshP);
            db.Orders.AddRange(expired, fresh);
        });

        var job = new ReleaseExpiredReservationsJob(
            ctx, Mock.Of<ILogger<ReleaseExpiredReservationsJob>>());

        await job.ReleaseAllExpiredAsync(default);

        // expired order: stock restored 5+3=8
        Assert.Equal(8, product.Stock);
        Assert.Equal(0, product.ReservedQuantity);
        Assert.Equal(Order.StatusFailed, (await ctx.Orders.FindAsync(expired.Id))!.Status);

        // fresh order: untouched
        Assert.Equal(10, freshP.Stock);
        Assert.Equal(Order.StatusPending, (await ctx.Orders.FindAsync(fresh.Id))!.Status);
    }
}
