using MediatR;
using NovaShop.Application.Features.Categories.Commands;
using NovaShop.Application.Features.Categories.Queries;

namespace NovaShop.Api.Endpoints;

public static class CategoriesEndpoints
{
    public static IEndpointRouteBuilder MapCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", async (IMediator mediator, string? searchTerm, int pageNumber = 1, int pageSize = 20) =>
        {
            var query = new GetCategoriesQuery { SearchTerm = searchTerm, PageNumber = pageNumber, PageSize = pageSize };
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetCategories").AllowAnonymous();

        app.MapGet("/api/categories/{id}", async (int id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCategoryQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetCategory").AllowAnonymous();

        app.MapPost("/api/categories", async (CreateCategoryCommand command, IMediator mediator) =>
        {
            var id = await mediator.Send(command);
            return Results.Created($"/api/categories/{id}", id);
        })
        .WithName("CreateCategory").RequireAuthorization("AdminOnly");

        app.MapPut("/api/categories/{id}", async (int id, UpdateCategoryCommand command, IMediator mediator) =>
        {
            command = command with { Id = id };
            var success = await mediator.Send(command);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateCategory").RequireAuthorization("AdminOnly");

        app.MapDelete("/api/categories/{id}", async (int id, IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteCategoryCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteCategory").RequireAuthorization("AdminOnly");

        return app;
    }
}
