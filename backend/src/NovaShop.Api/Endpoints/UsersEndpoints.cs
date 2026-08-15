using MediatR;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Application.Features.Users.Queries;

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
}
