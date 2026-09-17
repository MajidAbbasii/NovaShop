using MediatR;
using NovaShop.Application.Features.Admin.Queries;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Queries;

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
        app.MapGet("/api/admin/dashboard", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new AdminDashboardQuery());
            return Results.Ok(result);
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
            IMediator mediator, int? rating = null, int pageNumber = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new AdminReviewsQuery(rating, pageNumber, pageSize));
            return Results.Ok(result);
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
        .WithName("AdminGetSmsNotifications")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}
