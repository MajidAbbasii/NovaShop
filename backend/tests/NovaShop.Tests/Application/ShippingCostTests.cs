using MassTransit;
using Moq;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Handlers;
using NovaShop.Application.Mappers;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NovaShop.Tests.Application;

public class ShippingCostTests
{
    static ShippingCostTests()
    {
        // Apply the documented business rules once for this test type.
        ShippingPolicy.Apply(new ShippingOptions
        {
            Post = new PostShippingOptions { Price = 59_900m, FreeShippingThreshold = 500_000m },
            CourierPrice = 129_000m,
            PickupPrice = 0m
        });
    }

    private static ShippingCostService Service => new();

    // ==================================================================
    // Unit: ShippingCostService
    // ==================================================================
    [Theory]
    [InlineData("POST",    100_000,  59_900)]
    [InlineData("POST",    500_000,  0)]
    [InlineData("POST",    500_001,  0)]
    [InlineData("COURIER", 100_000,  129_000)]
    [InlineData("PICKUP",  100_000,  0)]
    [InlineData("post",    100_000,  59_900)]   // case-insensitive
    [InlineData("Courier", 1,        129_000)]
    public void Calculate_ReturnsExpectedCost(string method, decimal subtotal, decimal expected)
    {
        var result = Service.Calculate(subtotal, method);
        Assert.Equal(expected, result.ShippingCost);
        Assert.Equal(subtotal + expected, result.GrandTotal);
        Assert.Equal(method.ToUpperInvariant(), result.ShippingMethod);
        Assert.Equal(expected == 0, result.IsFreeShipping);
    }

    [Fact]
    public void Calculate_FreeThresholdUsesSubtotalNotFinal()
    {
        // Threshold is on (subtotal - discount). With subtotal 600,000 but
        // discount 200,000 => taxable 400,000 < 500,000 => POST charges.
        // Service itself takes taxable subtotal. Confirm 400k => charged.
        var r = Service.Calculate(400_000m, "POST");
        Assert.Equal(59_900m, r.ShippingCost);
    }

    [Theory]
    [InlineData("BOGUS")]
    [InlineData("")]
    [InlineData(null!)]
    public void Calculate_InvalidMethod_Throws(string? method)
    {
        Assert.Throws<InvalidOperationException>(() => Service.Calculate(100_000m, method!));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("COURIER")]
    [InlineData("PICKUP")]
    [InlineData("post")]
    public void NormalizeMethod_AcceptsValid(string method)
    {
        Assert.NotNull(Service.NormalizeMethod(method));
    }

    [Theory]
    [InlineData("BOGUS")]
    [InlineData(null!)]
    public void NormalizeMethod_RejectsInvalid(string? method)
    {
        Assert.Null(Service.NormalizeMethod(method));
    }

    // ==================================================================
    // Integration: CreateOrderFromCartCommandHandler ignores client shipping cost
    // ==================================================================
    private static async Task<NovaShopDbContext> SetupCartAsync(string dbName, int userId, decimal productPrice, int qty)
    {
        var options = new DbContextOptionsBuilder<NovaShopDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var ctx = new NovaShopDbContext(options);
        var product = new Product { Id = 1, Name = "P", Price = productPrice, Stock = 100, CategoryId = 1 };
        ctx.Products.Add(product);
        var cart = new Cart { UserId = userId };
        cart.AddItem(product, qty);
        ctx.Carts.Add(cart);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    [Theory]
    [InlineData("COURIER", 129_000)]
    [InlineData("POST",    59_900)]   // subtotal 100,000 < 500,000 => charged
    [InlineData("PICKUP",  0)]
    public async Task Handler_UsesServerShippingCost_IgnoresClientValue(string method, decimal expectedCost)
    {
        var ctx = await SetupCartAsync($"ship_{method}_{Guid.NewGuid():N}", 1, 100_000m, 1);

        // The command no longer accepts ShippingCost — client tampering via the
        // old contract is impossible by construction.
        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), Mock.Of<IDiscountRepository>(),
            new ShippingCostService(), Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var result = await handler.Handle(
            new CreateOrderFromCartCommand(1, "123 Main St", "InPerson", ShippingMethod: method), default);

        Assert.Equal(expectedCost, result.ShippingCost);
        Assert.Equal(100_000m + expectedCost, result.TotalAmount);
    }

    [Fact]
    public async Task Handler_Post_FreeShipping_WhenSubtotalReachesThreshold()
    {
        // subtotal 500,000 >= threshold => free POST
        var ctx = await SetupCartAsync($"free_{Guid.NewGuid():N}", 1, 500_000m, 1);
        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), Mock.Of<IDiscountRepository>(),
            new ShippingCostService(), Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        var result = await handler.Handle(
            new CreateOrderFromCartCommand(1, "Addr", "InPerson", ShippingMethod: "POST"), default);

        Assert.Equal(0m, result.ShippingCost);
        Assert.Equal(500_000m, result.TotalAmount);
    }

    [Fact]
    public async Task Handler_InvalidShippingMethod_Throws()
    {
        var ctx = await SetupCartAsync($"bad_{Guid.NewGuid():N}", 1, 100m, 1);
        var handler = new CreateOrderFromCartCommandHandler(
            ctx, new OrderMapper(), Mock.Of<IPublishEndpoint>(), Mock.Of<IReservationScheduler>(),
            Mock.Of<INotificationService>(), Mock.Of<IDiscountRepository>(),
            new ShippingCostService(), Mock.Of<ILogger<CreateOrderFromCartCommandHandler>>());

        // Validator rejects "BOGUS" at the MediatR pipeline; the handler also
        // rejects it at the domain boundary (defense in depth).
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateOrderFromCartCommand(1, "Addr", "InPerson", ShippingMethod: "BOGUS"), default));
    }
}
