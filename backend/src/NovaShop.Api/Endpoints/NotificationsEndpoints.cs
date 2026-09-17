using MediatR;
using NovaShop.Application.Features.Notifications.Commands;
using NovaShop.Application.Features.Notifications.Queries;

namespace NovaShop.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        // My in-app notifications
        app.MapGet("/api/notifications", async (
            HttpContext httpContext,
            IMediator mediator,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyNotificationsQuery(userId.Value, pageNumber, pageSize));
            return Results.Ok(new { items = result.Items, total = result.Total, pageNumber = result.PageNumber, pageSize = result.PageSize });
        })
        .WithName("GetMyNotifications")
        .RequireAuthorization();

        // Mark one notification read
        app.MapPost("/api/notifications/{id}/read", async (
            int id,
            HttpContext httpContext,
            IMediator mediator) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new MarkNotificationReadCommand(id, userId.Value));
            if (!result) return Results.NotFound();
            return Results.Ok();
        })
        .WithName("MarkNotificationRead")
        .RequireAuthorization();

        // Mark all read
        app.MapPost("/api/notifications/read-all", async (
            HttpContext httpContext,
            IMediator mediator) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var count = await mediator.Send(new MarkAllNotificationsReadCommand(userId.Value));
            return Results.Ok(new { updated = count });
        })
        .WithName("MarkAllNotificationsRead")
        .RequireAuthorization();

        // Unread count (for the header bell)
        app.MapGet("/api/notifications/unread-count", async (
            HttpContext httpContext,
            IMediator mediator) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetUnreadNotificationCountQuery(userId.Value));
            return Results.Ok(new { count = result.Count });
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
