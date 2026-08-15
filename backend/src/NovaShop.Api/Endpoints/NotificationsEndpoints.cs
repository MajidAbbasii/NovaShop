using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        // My in-app notifications
        app.MapGet("/api/notifications", async (
            HttpContext httpContext,
            NovaShopDbContext context,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = context.AppNotifications
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new AppNotificationDto
                {
                    Id = n.Id,
                    OrderId = n.OrderId,
                    CustomDollRequestId = n.CustomDollRequestId,
                    Type = n.Type,
                    Channel = n.Channel,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { items, total, pageNumber, pageSize });
        })
        .WithName("GetMyNotifications")
        .RequireAuthorization();

        // Mark one notification read
        app.MapPost("/api/notifications/{id}/read", async (
            int id,
            HttpContext httpContext,
            NovaShopDbContext context) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var notification = await context.AppNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);
            if (notification == null) return Results.NotFound();

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("MarkNotificationRead")
        .RequireAuthorization();

        // Mark all read
        app.MapPost("/api/notifications/read-all", async (
            HttpContext httpContext,
            NovaShopDbContext context) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var unread = await context.AppNotifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
            return Results.Ok(new { updated = unread.Count });
        })
        .WithName("MarkAllNotificationsRead")
        .RequireAuthorization();

        // Unread count (for the header bell)
        app.MapGet("/api/notifications/unread-count", async (
            HttpContext httpContext,
            NovaShopDbContext context) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var count = await context.AppNotifications
                .CountAsync(n => n.UserId == userId.Value && !n.IsRead);
            return Results.Ok(new { count });
        })
        .WithName("GetUnreadNotificationCount")
        .RequireAuthorization();

        return app;
    }

    private static int? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub");
        if (claim == null || !int.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}