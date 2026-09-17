using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Commands;
using NovaShop.Application.Features.CustomDollRequests.Queries;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class CustomDollRequestsEndpoints
{
    public static IEndpointRouteBuilder MapCustomDollRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        // Customer: create request
        app.MapPost("/api/custom-doll-requests", async (
            CreateCustomDollRequestCommand command,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            command = command with { UserId = userId.Value };
            var id = await mediator.Send(command);
            return Results.Created($"/api/custom-doll-requests/{id}", id);
        })
        .WithName("CreateCustomDollRequest")
        .RequireAuthorization();

        // Customer: my requests
        app.MapGet("/api/custom-doll-requests", async (
            ClaimsPrincipal user,
            IMediator mediator,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyCustomDollRequestsQuery(userId.Value, pageNumber, pageSize));
            return Results.Ok(new { items = result.Items, total = result.Total, pageNumber = result.PageNumber, pageSize = result.PageSize });
        })
        .WithName("GetMyCustomDollRequests")
        .RequireAuthorization();

        // Customer: my requests (explicit segment — must precede /{id})
        app.MapGet("/api/custom-doll-requests/my", async (
            ClaimsPrincipal user,
            IMediator mediator,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyCustomDollRequestsQuery(userId.Value, pageNumber, pageSize));
            return Results.Ok(new { items = result.Items, total = result.Total, pageNumber = result.PageNumber, pageSize = result.PageSize });
        })
        .WithName("GetMyCustomDollRequestsAlt")
        .RequireAuthorization();

        // Customer: request detail (own only)
        app.MapGet("/api/custom-doll-requests/{id}", async (
            int id,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyCustomDollRequestDetailQuery(id, userId.Value));
            if (result == null) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("GetMyCustomDollRequest")
        .RequireAuthorization();

        // Customer: accept approved price (final confirmation before crafting)
        app.MapPost("/api/custom-doll-requests/{id}/accept", async (
            int id,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new AcceptCustomDollRequestCommand(id, userId.Value));
            return Results.Ok(result);
        })
        .WithName("AcceptCustomDollRequest")
        .RequireAuthorization();

        // Admin: list all
        app.MapGet("/api/admin/custom-doll-requests", async (
            IMediator mediator,
            int pageNumber = 1,
            int pageSize = 50,
            string? status = null) =>
        {
            var result = await mediator.Send(new GetAllCustomDollRequestsQuery(pageNumber, pageSize, status));
            return Results.Ok(new { items = result.Items, total = result.Total, pageNumber = result.PageNumber, pageSize = result.PageSize });
        })
        .WithName("AdminGetCustomDollRequests")
        .RequireAuthorization("AdminOnly");

        // Admin: detail
        app.MapGet("/api/admin/custom-doll-requests/{id}", async (
            int id,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCustomDollRequestDetailQuery(id));
            if (result == null) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("AdminGetCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        // Admin: approve (sets price + message)
        app.MapPost("/api/admin/custom-doll-requests/{id}/approve", async (
            int id,
            ApproveCustomDollRequestCommand command,
            IMediator mediator) =>
        {
            command = command with { RequestId = id };
            await mediator.Send(command);
            return Results.Ok();
        })
        .WithName("ApproveCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        // Admin: reject
        app.MapPost("/api/admin/custom-doll-requests/{id}/reject", async (
            int id,
            RejectCustomDollRequestCommand command,
            IMediator mediator) =>
        {
            command = command with { RequestId = id };
            await mediator.Send(command);
            return Results.Ok();
        })
        .WithName("RejectCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        return app;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (claim == null || !int.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}
