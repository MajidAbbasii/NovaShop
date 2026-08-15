using MediatR;
using NovaShop.Application.Features.Wishlists.Commands;
using NovaShop.Application.Features.Wishlists.Queries;

namespace NovaShop.Api.Endpoints;

public static class WishlistEndpoints
{
    public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        // Add to wishlist
        app.MapPost("/api/wishlist", async (AddToWishlistRequest request, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new AddToWishlistCommand
            {
                UserId = userId.Value,
                ProductId = request.ProductId,
                Note = request.Note
            };

            var success = await mediator.Send(command);
            return success ? Results.Ok() : Results.BadRequest(new { error = "Product not found" });
        })
        .WithName("AddToWishlist")
        .RequireAuthorization();

        // Remove from wishlist
        app.MapDelete("/api/wishlist/{productId}", async (int productId, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var success = await mediator.Send(new RemoveFromWishlistCommand(userId.Value, productId));
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveFromWishlist")
        .RequireAuthorization();

        // Get user's wishlist
        app.MapGet("/api/wishlist", async (
            IMediator mediator,
            HttpContext httpContext,
            int pageNumber = 1,
            int pageSize = 12) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = new GetWishlistQuery(userId.Value, pageNumber, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetWishlist")
        .RequireAuthorization();

        // Check if product is in wishlist
        app.MapGet("/api/wishlist/check/{productId}", async (int productId, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new CheckWishlistItemQuery(userId.Value, productId));
            return Results.Ok(result);
        })
        .WithName("CheckWishlistItem")
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

public record AddToWishlistRequest(int ProductId, string? Note = null);
