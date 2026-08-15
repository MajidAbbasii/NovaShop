using MediatR;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Queries;

namespace NovaShop.Api.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        // Create Order from current user's cart
        app.MapPost("/api/orders", async (
            CreateOrderRequest request,
            IMediator mediator,
            HttpContext httpContext,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new CreateOrderFromCartCommand(
                UserId: userId.Value,
                ShippingAddress: request.ShippingAddress,
                PaymentMethod: request.PaymentMethod,
                ShippingMethod: request.ShippingMethod ?? "POST",
                ShippingCost: request.ShippingCost,
                PickupLocation: request.PickupLocation,
                PickupInstructions: request.PickupInstructions,
                PhoneNumber: request.PhoneNumber,
                IdempotencyKey: idempotencyKey,
                DiscountCode: request.DiscountCode
            );

            var order = await mediator.Send(command);
            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrderFromCart")
        .RequireAuthorization();

        // Process Payment for an order
        app.MapPost("/api/orders/{orderId}/pay", async (
            int orderId,
            IMediator mediator,
            HttpContext httpContext,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new ProcessPaymentCommand(
                OrderId: orderId,
                UserId: userId.Value,
                IdempotencyKey: idempotencyKey
            );

            try
            {
                var result = await mediator.Send(command);
                return result.Success
                    ? Results.Ok(result)
                    : Results.UnprocessableEntity(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ProcessPayment")
        .RequireAuthorization();

        // Get orders for the current user
        app.MapGet("/api/orders", async (
            IMediator mediator,
            HttpContext httpContext,
            int pageNumber = 1,
            int pageSize = 20) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = new GetOrdersQuery
            {
                UserId = userId.Value,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetMyOrders")
        .RequireAuthorization();

        // Get a specific order by id
        app.MapGet("/api/orders/{id}", async (
            int id,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = new GetOrderQuery(id);
            var order = await mediator.Send(query);

            if (order == null)
                return Results.NotFound();

            var isAdmin = httpContext.User.IsInRole("Admin");
            if (order.UserId != userId.Value && !isAdmin)
                return Results.Forbid();

            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .RequireAuthorization();

        // Cancel order (customer) — releases reserved stock per business rules
        app.MapPost("/api/orders/{id}/cancel", async (
            int id,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = new GetOrderQuery(id);
            var order = await mediator.Send(query);
            if (order == null) return Results.NotFound();
            if (order.UserId != userId.Value) return Results.Forbid();

            try
            {
                var result = await mediator.Send(new UpdateOrderStatusCommand(
                    OrderId: id,
                    Status: "Cancelled",
                    Note: "لغو توسط مشتری",
                    ChangedByUserId: userId.Value,
                    ChangedByRole: "Customer"));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CancelOrder")
        .RequireAuthorization();

        // Refund a paid order to the customer wallet (admin; or system on cancel)
        app.MapPost("/api/orders/{id}/refund", async (
            int id,
            [FromBody] RefundOrderRequest? request,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var isAdmin = httpContext.User.IsInRole("Admin");
            if (!isAdmin) return Results.Forbid();

            try
            {
                var result = await mediator.Send(new RefundOrderCommand(
                    OrderId: id,
                    UserId: userId.Value,
                    Reason: request?.Reason ?? "بازگشت وجه توسط مدیریت",
                    Amount: request?.Amount));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RefundOrder")
        .RequireAuthorization();

        // Customer requests return on a delivered order
        app.MapPost("/api/orders/{id}/return-request", async (
            int id,
            [FromBody] ReturnRequestModel? request,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var query = new GetOrderQuery(id);
            var order = await mediator.Send(query);
            if (order == null) return Results.NotFound();
            if (order.UserId != userId.Value) return Results.Forbid();

            try
            {
                var result = await mediator.Send(new UpdateOrderStatusCommand(
                    OrderId: id,
                    Status: "ReturnRequested",
                    Note: request?.Reason ?? "درخواست مرجوعی",
                    ChangedByUserId: userId.Value,
                    ChangedByRole: "Customer"));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RequestOrderReturn")
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

// Request DTOs
public record CreateOrderRequest(
    string ShippingAddress,
    string PaymentMethod,
    string ShippingMethod = "POST",
    decimal? ShippingCost = null,
    string? PickupLocation = null,
    string? PickupInstructions = null,
    string? PhoneNumber = null,
    string? DiscountCode = null
);

public record RefundOrderRequest(string? Reason = null, decimal? Amount = null);

public record ReturnRequestModel(string? Reason = null);
