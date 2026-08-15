using MediatR;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Application.Features.Products.Dtos;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Domain.Common;

namespace NovaShop.Api.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products", async (IMediator mediator, string? searchTerm, decimal? minPrice, decimal? maxPrice, bool? onlyAvailable, int? categoryId, int pageNumber = 1, int pageSize = 12) =>
        {
            var query = new GetProductsQuery
            {
                SearchTerm = searchTerm,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                OnlyAvailable = onlyAvailable,
                CategoryId = categoryId,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetProducts")
        .AllowAnonymous();

        app.MapGet("/api/products/{id}", async (int id, IMediator mediator) =>
        {
            var query = new GetProductByIdQuery { Id = id };
            var product = await mediator.Send(query);
            return product is null ? Results.NotFound() : Results.Ok(product);
        })
        .WithName("GetProductById")
        .AllowAnonymous();

        app.MapPost("/api/products", async (CreateProductCommand command, IMediator mediator) =>
        {
            var productId = await mediator.Send(command);
            return Results.Created($"/api/products/{productId}", productId);
        })
        .WithName("CreateProduct")
        .RequireAuthorization("AdminOnly");

        app.MapPut("/api/products/{id}", async (int id, UpdateProductCommand command, IMediator mediator) =>
        {
            command = command with { Id = id };
            var success = await mediator.Send(command);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateProduct")
        .RequireAuthorization("AdminOnly");

        app.MapDelete("/api/products/{id}", async (int id, IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteProductCommand { Id = id });
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .RequireAuthorization("AdminOnly");

        // Full-text search
        app.MapGet("/api/products/search", async (
            IMediator mediator,
            [AsParameters] SearchProductsQuery query) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("SearchProducts")
        .AllowAnonymous();

        // Auto-complete suggestions
        app.MapGet("/api/products/suggestions", async (
            IMediator mediator,
            string q,
            int max = 8) =>
        {
            var result = await mediator.Send(new GetProductSuggestionsQuery(q, max));
            return Results.Ok(result);
        })
        .WithName("ProductSuggestions")
        .AllowAnonymous();

        return app;
    }
}
