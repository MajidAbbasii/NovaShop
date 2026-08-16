using MediatR;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Application.Features.Users.Queries;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", async (IMediator mediator,
            int pageNumber = 1, int pageSize = 20,
            string? searchTerm = null, string? role = null) =>
        {
            var query = new GetUsersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Role = role
            };
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetUsers").RequireAuthorization("AdminOnly");

        app.MapGet("/api/users/{id}", async (int id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetUserQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetUser").RequireAuthorization("AdminOnly");

        // Customer self-profile (authenticated, own record only).
        app.MapGet("/api/users/me", async (IMediator mediator, ClaimsPrincipal user) =>
        {
            var id = GetUserId(user);
            if (id == null) return Results.Unauthorized();
            var result = await mediator.Send(new GetUserQuery(id.Value));
            return Results.Ok(result);
        })
        .WithName("GetCurrentUser").RequireAuthorization();

        app.MapPut("/api/users/me", async (UpdateProfileCommand command, IMediator mediator, ClaimsPrincipal user) =>
        {
            var id = GetUserId(user);
            if (id == null) return Results.Unauthorized();
            // Force the id from the authenticated principal so customers cannot edit others.
            command = command with { UserId = id.Value };
            var success = await mediator.Send(command);
            return success ? Results.Ok(await mediator.Send(new GetUserQuery(id.Value))) : Results.NotFound();
        })
        .WithName("UpdateCurrentUser").RequireAuthorization();

        app.MapPost("/api/users", async (CreateUserCommand command, IMediator mediator) =>
        {
            var id = await mediator.Send(command);
            return Results.Created($"/api/users/{id}", id);
        })
        .WithName("CreateUser").RequireAuthorization("AdminOnly");

        app.MapPut("/api/users/{id}", async (int id, UpdateUserCommand command, IMediator mediator) =>
        {
            command = command with { Id = id };
            var success = await mediator.Send(command);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateUser").RequireAuthorization("AdminOnly");

        app.MapDelete("/api/users/{id}", async (int id, IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteUserCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteUser").RequireAuthorization("AdminOnly");

        return app;
    }

    /// <summary>
    /// Extracts the numeric user id from the JWT NameIdentifier claim.
    /// Returns null when missing or unparsable (caller should return 401).
    /// </summary>
    private static int? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
