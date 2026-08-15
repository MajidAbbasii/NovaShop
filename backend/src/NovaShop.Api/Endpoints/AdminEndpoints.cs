using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Features.Orders.Queries;
using NovaShop.Domain.Common;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all orders (admin)
        app.MapGet("/api/admin/orders", async (IMediator mediator,
            int pageNumber = 1, int pageSize = 20,
            string? searchTerm = null, string? status = null) =>
        {
            var query = new GetOrdersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("AdminGetOrders").RequireAuthorization("AdminOnly");

        // Get single order with full timeline (admin)
        app.MapGet("/api/admin/orders/{orderId}", async (int orderId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderQuery(orderId));
            if (result.Id == 0) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("AdminGetOrder").RequireAuthorization("AdminOnly");

        // Update order status (admin) — returns full OrderDto with timeline
        app.MapPut("/api/admin/orders/{orderId}/status", async (
            int orderId, UpdateOrderStatusCommand command, IMediator mediator) =>
        {
            command = command with { OrderId = orderId };
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("AdminUpdateOrderStatus").RequireAuthorization("AdminOnly");

        // Dashboard stats
        app.MapGet("/api/admin/dashboard", async (NovaShopDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var totalUsers = await db.Users.CountAsync();
            var totalOrders = await db.Orders.CountAsync();
            var pendingOrders = await db.Orders.CountAsync(o => o.Status == "Pending");
            var revenue = await db.Orders
                .Where(o => o.Status == "Delivered" || o.Status == "Shipped")
                .SumAsync(o => o.TotalAmount);

            var dailyRevenue = await db.Orders
                .Where(o => o.CreatedAt >= now.AddDays(-7) && (o.Status == "Delivered" || o.Status == "Shipped"))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var recentOrders = await db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    o.TotalAmount,
                    o.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                totalUsers,
                totalOrders,
                pendingOrders,
                revenue,
                dailyRevenue,
                recentOrders
            });
        })
        .WithName("AdminDashboard").RequireAuthorization("AdminOnly");

        // Inventory transaction ledger (admin)
        app.MapGet("/api/admin/inventory", async (IMediator mediator,
                    string? type = null, int? productId = null, int? orderId = null,
                    int pageNumber = 1, int pageSize = 20) =>
                {
                    var query = new GetInventoryTransactionsQuery(productId, orderId, type, pageNumber, pageSize);
                    return Results.Ok(await mediator.Send(query));
                })
                .WithName("AdminGetInventory").RequireAuthorization("AdminOnly");

                // list all reviews for moderation (admin)
                app.MapGet("/api/admin/reviews", async (
                    NovaShopDbContext db, int? rating = null, int pageNumber = 1, int pageSize = 20) =>
                {
                    var query = db.Reviews
                        .Include(r => r.Product)
                        .Include(r => r.User)
                        .AsNoTracking();

                    if (rating is >= 1 and <= 5)
                        query = query.Where(r => r.Rating == rating.Value);

                    var totalCount = await query.CountAsync();
                    var items = await query
                        .OrderByDescending(r => r.CreatedAt)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(r => new
                        {
                            r.Id,
                            r.ProductId,
                            ProductName = r.Product.Name,
                            r.UserId,
                            UserName = r.User.Username,
                            r.Rating,
                            r.Comment,
                            r.CreatedAt
                        })
                        .ToListAsync();

                    return Results.Ok(new
                    {
                        items,
                        totalCount,
                        pageNumber,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    });
                })
                .WithName("AdminGetReviews").RequireAuthorization("AdminOnly");

                        // SMS notification log (admin)
        app.MapGet("/api/admin/notifications/sms", async (IMediator mediator,
            int? orderId = null, string? status = null,
            int pageNumber = 1, int pageSize = 50) =>
        {
            var query = new GetSmsNotificationsQuery(orderId, status, pageNumber, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("AdminGetSmsNotifications").RequireAuthorization("AdminOnly");

        return app;
    }
}
