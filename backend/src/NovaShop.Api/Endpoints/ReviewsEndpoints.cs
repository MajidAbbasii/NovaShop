using System.Security.Claims;
using MediatR;
using NovaShop.Application.Features.Reviews.Commands;
using NovaShop.Application.Features.Reviews.Queries;

namespace NovaShop.Api.Endpoints;

public static class ReviewsEndpoints
{
    public static IEndpointRouteBuilder MapReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/{productId}/reviews", async (int productId, IMediator mediator) =>
        {
            var list = await mediator.Send(new GetReviewsByProductQuery(productId));
            return Results.Ok(list);
        }).WithName("GetReviewsForProduct").AllowAnonymous();

        app.MapPost("/api/reviews", async (CreateReviewCommand command, HttpContext httpContext, IMediator mediator) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();
            command.UserId = userId.Value;
            var id = await mediator.Send(command);
            return Results.Created($"/api/reviews/{id}", id);
        }).WithName("CreateReview").RequireAuthorization();

        app.MapDelete("/api/reviews/{id}", async (int id, HttpContext httpContext, IMediator mediator) =>
        {
            var userId = GetUserId(httpContext);
            var success = await mediator.Send(new DeleteReviewCommand(id, userId));
            return success ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteReview").RequireAuthorization();

        return app;
    }

    private static int? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? httpContext.User.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }
}
