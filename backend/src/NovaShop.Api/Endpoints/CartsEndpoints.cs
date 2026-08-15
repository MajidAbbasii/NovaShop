using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Application.Features.Carts.Queries;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // Get Cart
        app.MapGet("/api/cart", async (IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();
            var query = new GetCartQuery(userId.Value);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetCart")
        .RequireAuthorization();

        // Add to Cart
        app.MapPost("/api/cart", async (AddToCartRequest request, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();
            var cmd = new AddToCartCommand(userId.Value, request.ProductId, request.Quantity, request.ProductColorId);
            var success = await mediator.Send(cmd);
            return success ? Results.Ok() : Results.BadRequest("Unable to add to cart");
        })
        .WithName("AddToCart")
        .RequireAuthorization();

        // Update cart item quantity
        app.MapPut("/api/cart/items/{cartItemId:int}", async (int cartItemId, UpdateCartItemRequest request, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new UpdateCartItemCommand(userId.Value, cartItemId, request.Quantity);
            var success = await mediator.Send(command);
            return success ? Results.Ok() : Results.BadRequest("Unable to update cart item");
        })
        .WithName("UpdateCartItem")
        .RequireAuthorization();

        // Remove cart item
        app.MapDelete("/api/cart/items/{cartItemId:int}", async (int cartItemId, IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new RemoveCartItemCommand(userId.Value, cartItemId);
            var success = await mediator.Send(command);
            return success ? Results.Ok() : Results.BadRequest("Unable to remove cart item");
        })
        .WithName("RemoveCartItem")
        .RequireAuthorization();

        // Clear Cart
        app.MapDelete("/api/cart", async (IMediator mediator, HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();
            var command = new ClearCartCommand(userId.Value);
            var success = await mediator.Send(command);
            return success ? Results.Ok() : Results.BadRequest();
        })
        .WithName("ClearCart")
        .RequireAuthorization();

        return app;
    }

    private static int? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? httpContext.User.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }
}

public record UpdateCartItemRequest(int Quantity);
public record AddToCartRequest(int ProductId, int Quantity, int? ProductColorId = null);
