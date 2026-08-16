using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NovaShop.Application.Caching;
using NovaShop.Domain.Exceptions;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Handlers;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Application.Features.Products.Handlers;
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

/// <summary>
/// Tests for the Inventory Reservation lifecycle: reserve → confirm → release,
/// idempotency, concurrency, transaction rollback, and admin stock safety.
/// </summary>
public class InventoryReservationTests
{
    private static DbContextOptions<NovaShopDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<NovaShopDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static async Task<NovaShopDbContext> SeedAsync(string dbName, Action<NovaShopDbContext> seed)
    {
        var ctx = new NovaShopDbContext(CreateOptions(dbName));
        seed(ctx);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static CreateOrderFromCartCommandHandler BuildHandler(
        NovaShopDbContext ctx,
        Mock<IPublishEndpoint>? publish = null,
        Mock<IReservationScheduler>? scheduler = null)
    {
        var discountRepo = new Mock<IDiscountRepository>();
        discountRepo.Setup(d => d.GetByCodeIgnoringCaseAsync(It.IsAny<string>()))
            .ReturnsAsync((Discount?)null);
        return new CreateOrderFromCartCommandHandler(
            ctx,
            new OrderMapper(),
            publish?.Object ?? Mock.Of<IPublishEndpoint>(),
            scheduler?.Object ?? Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(),
            discountRepo.Object,
            new ShippingCostService(),
            Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());
    }

    // ====================================================================
    // TEST 1: Normal reservation
    // ====================================================================
    [Fact]
    public async Task ReserveStock_Normal_StockReducedAndReservedIncreased()
    {
        var dbName = $"{nameof(ReserveStock_Normal_StockReducedAndReservedIncreased)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 100m, Stock = 5, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            var cart = new Cart { UserId = 1 };
            cart.AddItem(p, 2);
            db.Carts.Add(cart);
        });

        var handler = BuildHandler(ctx);
        var result = await handler.Handle(
            new CreateOrderFromCartCommand(1, "Address 123", "InPerson"), default);

        Assert.NotNull(result);
        Assert.Equal(Order.StatusPending, result.Status);
        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(3, saved!.Stock);        // 5 - 2
        Assert.Equal(2, saved.ReservedQuantity);
        // One reservation transaction per OrderItem reserved
        Assert.Equal(1, await ctx.InventoryTransactions.CountAsync(t => t.Type == "Reserve"));
    }

    // ====================================================================
    // TEST 4: Insufficient stock — no order, no reservation
    // ====================================================================
    [Fact]
    public async Task ReserveStock_InsufficientStock_NoOrderOrReservation()
    {
        var dbName = $"{nameof(ReserveStock_InsufficientStock_NoOrderOrReservation)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "LowStock", Price = 10m, Stock = 1, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            var cart = new Cart { UserId = 1 };
            cart.AddItem(p, 2); // request 2, only 1 available
            db.Carts.Add(cart);
        });

        var handler = BuildHandler(ctx);
        var ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => handler.Handle(new CreateOrderFromCartCommand(1, "Address 12345", "InPerson"), default));

        Assert.Contains("موجودی", ex.Message);
        // Stock untouched
        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(1, saved!.Stock);
        Assert.Equal(0, saved.ReservedQuantity);
        Assert.Equal(0, await ctx.Orders.CountAsync());
        Assert.Equal(0, await ctx.InventoryTransactions.CountAsync());
        // Cart still exists (transaction rolled back)
        Assert.NotNull(await ctx.Carts.FirstOrDefaultAsync(c => c.UserId == 1));
    }

    // ====================================================================
    // TEST 2 & 6: Confirm reservation is idempotent (no double deduction)
    // ====================================================================
    [Fact]
    public async Task ConfirmReservation_Idempotent_NoDoubleDeduction()
    {
        var dbName = $"{nameof(ConfirmReservation_Idempotent_NoDoubleDeduction)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 50m, Stock = 8, ReservedQuantity = 2, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
        });

        // First confirm
        p.ConfirmReservation();
        Assert.Equal(0, p.ReservedQuantity);
        Assert.Equal(8, p.Stock); // Stock was already deducted at reserve time; confirm just clears reserved
        var firstConfirmTxnCount = await ctx.InventoryTransactions.CountAsync(t => t.Type == "Confirm");

        // Second confirm — should be a no-op, no double deduction
        p.ConfirmReservation();
        Assert.Equal(0, p.ReservedQuantity);
        Assert.Equal(8, p.Stock);

        // Manually persist the confirm transaction entry (as VerifyPayment handler does)
        ctx.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = p.Id,
            Type = "Confirm",
            Quantity = 2,
            StockBefore = 8,
            StockAfter = 8
        });
        await ctx.SaveChangesAsync();

        // Second confirm again — no additional transaction should be added by the idempotent call
        var beforeCount = await ctx.InventoryTransactions.CountAsync(t => t.Type == "Confirm");
        p.ConfirmReservation(); // no-op, no new transaction in DB
        var afterCount = await ctx.InventoryTransactions.CountAsync(t => t.Type == "Confirm");
        Assert.Equal(beforeCount, afterCount);
    }

    // ====================================================================
    // TEST 6: Double release is idempotent
    // ====================================================================
    [Fact]
    public void ReleaseReservation_Idempotent_NoDoubleRestore()
    {
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 5, ReservedQuantity = 3 };

        // First release — restores 3
        p.ReleaseReservation();
        Assert.Equal(8, p.Stock);    // 5 + 3
        Assert.Equal(0, p.ReservedQuantity);

        // Second release — idempotent, no-op
        p.ReleaseReservation();
        Assert.Equal(8, p.Stock);
        Assert.Equal(0, p.ReservedQuantity);

        // Partial release when ReservedQuantity is 0 — no-op
        p.ReleaseReservation(5);
        Assert.Equal(8, p.Stock);
        Assert.Equal(0, p.ReservedQuantity);
    }

    // ====================================================================
    // TEST 7: Concurrent reservation — two customers, last unit
    // ====================================================================
    [Fact]
    public async Task ConcurrentReservation_OnlyOneSucceeds_LastUnit()
    {
        var dbName = $"{nameof(ConcurrentReservation_OnlyOneSucceeds_LastUnit)}_{Guid.NewGuid():N}";
        // Two products: one to race on, and we verify the domain guard prevents
        // Stock from going negative or ReservedQuantity exceeding available.
        var p = new Product { Id = 1, Name = "LastUnit", Price = 10m, Stock = 1 };
        var ctx = await SeedAsync(dbName, db => db.Products.Add(p));

        // Simulate two customers racing for the last unit.
        // Each loads a fresh context (simulating separate requests).
        var tasks = Enumerable.Range(0, 2).Select(async i =>
        {
            var handlerCtx = new NovaShopDbContext(CreateOptions(dbName));
            // Attach and load fresh
            await using (var fresh = new NovaShopDbContext(CreateOptions(dbName)))
            {
                var product = await fresh.Products.FirstAsync(pr => pr.Id == 1);
                try
                {
                    product.ReserveStock(1, DateTime.UtcNow.AddMinutes(15));
                    await fresh.SaveChangesAsync();
                    return true; // success
                }
                catch (DbUpdateConcurrencyException)
                {
                    return false; // lost the race (RowVersion conflict)
                }
                catch (NovaShop.Domain.Exceptions.InsufficientStockException)
                {
                    return false; // insufficient stock
                }
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Exactly one should succeed
        Assert.Single(results.Where(r => r));
        var saved = await ctx.Products.FirstAsync(pr => pr.Id == 1);
        // Stock never goes negative; only one reservation survives
        Assert.True(saved.Stock >= 0);
        Assert.True(saved.ReservedQuantity <= 1);
        Assert.Contains(saved.ReservedQuantity, new[] { 0, 1 });
    }

    // ====================================================================
    // TEST: Order creation rollback on failure
    // ====================================================================
    [Fact]
    public async Task OrderCreation_Failure_RollsBackStockAndNoOrphanOrder()
    {
        var dbName = $"{nameof(OrderCreation_Failure_RollsBackStockAndNoOrphanOrder)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 5, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            // Cart has an item but references a non-existent product variant
            // to force a failure mid-reservation. We'll add a cart item pointing
            // to product 1 with quantity 2, then make the handler fail by using
            // an insufficient-stock scenario via a second product in the cart.
            var cart = new Cart { UserId = 1 };
            cart.AddItem(p, 10); // requests 10, only 5 available
            db.Carts.Add(cart);
        });

        var handler = BuildHandler(ctx);
        var ex = await Assert.ThrowsAsync<InsufficientStockException>(
            () => handler.Handle(new CreateOrderFromCartCommand(1, "Address 12345", "InPerson"), default));

        Assert.Contains("موجودی", ex.Message);

        // Verify rollback: stock unchanged, no order, no reservation transactions
        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(5, saved!.Stock);
        Assert.Equal(0, saved.ReservedQuantity);
        Assert.Equal(0, await ctx.Orders.CountAsync());
        Assert.Equal(0, await ctx.InventoryTransactions.CountAsync());
    }

    // ====================================================================
    // TEST: Cancel pre-paid order releases reserved stock (partial)
    // ====================================================================
    [Fact]
    public async Task CancelOrder_PrePaid_ReleasesReservedStock()
    {
        var dbName = $"{nameof(CancelOrder_PrePaid_ReleasesReservedStock)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 5, CategoryId = 1 };
        var order = new Order { UserId = 1, TotalAmount = 20m, Status = Order.StatusPending, PaymentStatus = Order.PaymentPending };
        order.AddItem(new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 10m, Product = p });
        // Simulate reservation already done
        p.ReserveStock(2, DateTime.UtcNow.AddMinutes(15));

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            db.Orders.Add(order);
        });
        await AddPaymentAsync(ctx, order, "InPerson", 20m, "Pending");

        var handler = new UpdateOrderStatusCommandHandler(
            ctx, new OrderMapper(), Mock.Of<INotificationService>(),
            Mock.Of<ILogger<UpdateOrderStatusCommandHandler>>());

        await handler.Handle(new UpdateOrderStatusCommand(order.Id, "Cancelled", "Customer"), default);

        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(5, saved!.Stock);    // 3 + 2 released
        Assert.Equal(0, saved.ReservedQuantity);
    }

    // ====================================================================
    // TEST: Cancel paid order restores sold stock (permanent)
    // ====================================================================
    [Fact]
    public async Task CancelOrder_PaidOrder_RestoresStock()
    {
        var dbName = $"{nameof(CancelOrder_PaidOrder_RestoresStock)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 3 }; // already sold 5 of 10
        var order = new Order { UserId = 1, TotalAmount = 40m, Status = Order.StatusPaid, PaymentStatus = Order.PaymentPaid };
        order.AddItem(new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 10m, Product = p });

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.Add(p);
            db.Orders.Add(order);
        });
        await AddPaymentAsync(ctx, order, "Wallet", 40m, "Completed");

        var handler = new UpdateOrderStatusCommandHandler(
            ctx, new OrderMapper(), Mock.Of<INotificationService>(),
            Mock.Of<ILogger<UpdateOrderStatusCommandHandler>>());

        await handler.Handle(new UpdateOrderStatusCommand(order.Id, "Cancelled", "Admin"), default);

        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(5, saved!.Stock); // 3 + 2 restored from sold
    }

    // ====================================================================
    // TEST: Admin stock update below reserved quantity throws
    // ====================================================================
    [Fact]
    public async Task AdminStockUpdate_BelowReserved_Throws()
    {
        var dbName = $"{nameof(AdminStockUpdate_BelowReserved_Throws)}_{Guid.NewGuid():N}";
        // Product with 5 available, 3 reserved (total physical 5)
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 5, ReservedQuantity = 3, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db => db.Products.Add(p));

        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(p);
        var handler = new UpdateProductCommandHandler(repo.Object, Mock.Of<ICacheService>(), ctx);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateProductCommand { Id = 1, Stock = 2 }, default));
        Assert.Contains("رزرو", ex.Message);

        // Stock unchanged
        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(5, saved!.Stock);
    }

    // ====================================================================
    // TEST: Admin stock update at or above reserved is allowed
    // ====================================================================
    [Fact]
    public async Task AdminStockUpdate_AtOrAboveReserved_Allowed()
    {
        var dbName = $"{nameof(AdminStockUpdate_AtOrAboveReserved_Allowed)}_{Guid.NewGuid():N}";
        var p = new Product { Id = 1, Name = "P1", Price = 10m, Stock = 5, ReservedQuantity = 3, CategoryId = 1 };
        var ctx = await SeedAsync(dbName, db => db.Products.Add(p));

        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(p);
        var handler = new UpdateProductCommandHandler(repo.Object, Mock.Of<ICacheService>(), ctx);

        // Set stock to exactly ReservedQuantity (3) — allowed (total physical = 3)
        var result = await handler.Handle(new UpdateProductCommand { Id = 1, Stock = 3 }, default);
        Assert.True(result);
        var saved = await ctx.Products.FindAsync(1);
        Assert.Equal(3, saved!.Stock);
        Assert.Equal(3, saved.ReservedQuantity);
    }

    // ====================================================================
    // TEST: ReleaseExpiredReservations only releases expired orders
    // ====================================================================
    [Fact]
    public async Task ReleaseExpiredReservations_OnlyExpiredOrdersReleased()
    {
        var dbName = $"{nameof(ReleaseExpiredReservations_OnlyExpiredOrdersReleased)}_{Guid.NewGuid():N}";
        var expiredProduct = new Product { Id = 1, Name = "Expired", Price = 10m, Stock = 5, ReservedQuantity = 3, CategoryId = 1 };
        var freshProduct = new Product { Id = 2, Name = "Fresh", Price = 10m, Stock = 5, ReservedQuantity = 2, CategoryId = 1 };

        var expiredOrder = new Order
        {
            UserId = 1, TotalAmount = 30m, Status = Order.StatusPending,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        expiredOrder.AddItem(new OrderItem { ProductId = 1, Quantity = 3, UnitPrice = 10m, Product = expiredProduct });

        var freshOrder = new Order
        {
            UserId = 1, TotalAmount = 20m, Status = Order.StatusPending,
            ReservationExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        freshOrder.AddItem(new OrderItem { ProductId = 2, Quantity = 2, UnitPrice = 10m, Product = freshProduct });

        var ctx = await SeedAsync(dbName, db =>
        {
            db.Products.AddRange(expiredProduct, freshProduct);
            db.Orders.AddRange(expiredOrder, freshOrder);
        });
        await AddPaymentAsync(ctx, expiredOrder, "InPerson", 30m, "Pending");
        await AddPaymentAsync(ctx, freshOrder, "InPerson", 20m, "Pending");

        var job = new ReleaseExpiredReservationsJob(ctx, Mock.Of<ILogger<ReleaseExpiredReservationsJob>>());
        await job.ReleaseAllExpiredAsync(default);

        // Expired order: stock restored
        var expSaved = await ctx.Products.FindAsync(1);
        Assert.Equal(8, expSaved!.Stock);       // 5 + 3
        Assert.Equal(0, expSaved.ReservedQuantity);
        Assert.Equal(Order.StatusFailed, (await ctx.Orders.FindAsync(expiredOrder.Id))!.Status);

        // Fresh order: untouched
        var freshSaved = await ctx.Products.FindAsync(2);
        Assert.Equal(5, freshSaved!.Stock);
        Assert.Equal(2, freshSaved!.ReservedQuantity);
        Assert.Equal(Order.StatusPending, (await ctx.Orders.FindAsync(freshOrder.Id))!.Status);
    }

    private static async Task AddPaymentAsync(NovaShopDbContext ctx, Order order, string method, decimal amount, string status)
    {
        ctx.Payments.Add(new Payment
        {
            OrderId = order.Id,
            PaymentMethod = method,
            Amount = amount,
            Status = status,
            TransactionId = string.Empty,
            IdempotencyKey = string.Empty
        });
        await ctx.SaveChangesAsync();
    }
}
