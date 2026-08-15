using MediatR;
using NovaShop.Application.Features.Discounts.Commands;
using NovaShop.Application.Features.Discounts.Queries;
namespace NovaShop.Api.Endpoints;

public static class DiscountsEndpoints
{
    public static IEndpointRouteBuilder MapDiscountsEndpoints(this IEndpointRouteBuilder app)
    {
        // === Admin Discount CRUD ===

        // POST /api/admin/discounts — create
        app.MapPost("/api/admin/discounts", async (
            CreateDiscountCommand command,
            IMediator mediator) =>
        {
            try
            {
                var id = await mediator.Send(command);
                return Results.Created($"/api/admin/discounts/{id}", id);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateDiscount")
        .RequireAuthorization("AdminOnly");

        // PUT /api/admin/discounts/{id} — update
        app.MapPut("/api/admin/discounts/{id}", async (
            int id,
            UpdateDiscountCommand command,
            IMediator mediator) =>
        {
            command = command with { Id = id };
            var success = await mediator.Send(command);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateDiscount")
        .RequireAuthorization("AdminOnly");

        // GET /api/admin/discounts — list
        app.MapGet("/api/admin/discounts", async (
            IMediator mediator,
            int pageNumber = 1,
            int pageSize = 20) =>
        {
            var query = new GetDiscountsQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetDiscounts")
        .RequireAuthorization("AdminOnly");

        // DELETE /api/admin/discounts/{id} — delete
        app.MapDelete("/api/admin/discounts/{id}", async (
            int id,
            IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteDiscountCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteDiscount")
        .RequireAuthorization("AdminOnly");

        // === Apply/Remove Discount on Order ===

        // POST /api/orders/apply-discount
        app.MapPost("/api/orders/apply-discount", async (
            ApplyDiscountRequest request,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var command = new ApplyDiscountToOrderCommand(
                    UserId: userId.Value,
                    OrderId: request.OrderId,
                    DiscountCode: request.DiscountCode
                );
                var order = await mediator.Send(command);
                return Results.Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ApplyDiscount")
        .RequireAuthorization();

        // DELETE /api/orders/{orderId}/discount — remove discount
        app.MapDelete("/api/orders/{orderId}/discount", async (
            int orderId,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var command = new RemoveDiscountFromOrderCommand(
                    UserId: userId.Value,
                    OrderId: orderId
                );
                var order = await mediator.Send(command);
                return Results.Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RemoveDiscount")
        .RequireAuthorization();

        // GET /api/discounts/validate?code=X — storefront check (auth optional)
        app.MapGet("/api/discounts/validate", async (
            string code,
            IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new ValidateDiscountQuery(code));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ValidateDiscount")
        .AllowAnonymous();

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

public record ApplyDiscountRequest(int OrderId, string DiscountCode);
