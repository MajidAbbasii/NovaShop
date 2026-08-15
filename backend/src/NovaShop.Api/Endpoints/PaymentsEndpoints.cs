using MediatR;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Queries;
using NovaShop.Infrastructure.Services;

namespace NovaShop.Api.Endpoints;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        // Server-side payment verification (gateway callback). The ONLY way an
        // order becomes Paid for online payments. Never trust browser redirects.
        app.MapPost("/api/payments/verify", async (
            VerifyPaymentRequest request,
            IMediator mediator,
            HttpContext httpContext,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null) =>
        {
            var command = new VerifyPaymentCommand(
                Authority: request.Authority,
                UserId: null,
                IdempotencyKey: idempotencyKey);

            var result = await mediator.Send(command);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        })
        .WithName("VerifyPayment")
        .AllowAnonymous();

        // Mock gateway: mark a session as paid (simulates the PSP's bank page
        // after "successful payment").
        app.MapPost("/api/mock-gateway/{authority}/complete", (
            string authority,
            [FromServices] MockPaymentStore store) =>
        {
            var session = store.Get(authority);
            if (session == null) return Results.NotFound(new { error = "پرداخت یافت نشد" });
            store.MarkPaid(authority, "paid");
            return Results.Ok(new { authority, status = "PAID", redirect = session.CallbackUrl });
        })
        .WithName("MockGatewayComplete")
        .AllowAnonymous();

        // Mock gateway: mark a cancelled payment.
        app.MapPost("/api/mock-gateway/{authority}/cancel", (
            string authority,
            [FromServices] MockPaymentStore store) =>
        {
            var session = store.Get(authority);
            if (session == null) return Results.NotFound(new { error = "پرداخت یافت نشد" });
            store.MarkCancelled(authority);
            return Results.Ok(new { authority, status = "CANCELLED" });
        })
        .WithName("MockGatewayCancel")
        .AllowAnonymous();

        // List active mock sessions (dev aid)
        app.MapGet("/api/mock-gateway/sessions", ([FromServices] MockPaymentStore store) =>
            Results.Ok(store.All().Select(s => new
            {
                s.Authority, s.Amount, s.OrderReference, s.Status, s.CallbackUrl
            })))
        .WithName("MockGatewaySessions")
        .AllowAnonymous();

        return app;
    }
}

public record VerifyPaymentRequest(string Authority);