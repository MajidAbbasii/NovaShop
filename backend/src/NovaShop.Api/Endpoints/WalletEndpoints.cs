using MediatR;
using NovaShop.Application.Features.Wallets.Queries;
using NovaShop.Common.Models;
using NovaShop.Application.Features.Orders.Commands;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        // Get my wallet + transactions
        app.MapGet("/api/wallet", async (
            IMediator mediator,
            HttpContext httpContext) =>
        {
            if (!PaymentPolicy.WalletEnabled)
                return Results.Problem(
                    detail: "کیف پول در حال حاضر غیرفعال است و در دسترس نیست.",
                    title: "Wallet Disabled",
                    statusCode: StatusCodes.Status403Forbidden);
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyWalletQuery(userId.Value, 50));
            return Results.Ok(result);
        })
        .WithName("GetMyWallet")
        .RequireAuthorization();

        // Charge wallet (initiates gateway payment)
        app.MapPost("/api/wallet/charge", async (
            ChargeWalletRequest request,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            if (!PaymentPolicy.WalletEnabled)
                return Results.Problem(
                    detail: "کیف پول در حال حاضر غیرفعال است و در دسترس نیست.",
                    title: "Wallet Disabled",
                    statusCode: StatusCodes.Status403Forbidden);
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var command = new ChargeWalletCommand(
                UserId: userId.Value,
                Amount: request.Amount,
                CallbackUrl: request.CallbackUrl);

            try
            {
                var result = await mediator.Send(command);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ChargeWallet")
        .RequireAuthorization();

        // Wallet recharge verification (gateway callback)
        app.MapPost("/api/wallet/verify", async (
            VerifyWalletChargeRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyWalletChargeCommand(request.Authority));
            return result.Success ? Results.Ok(result) : Results.UnprocessableEntity(result);
        })
        .WithName("VerifyWalletCharge")
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

public record ChargeWalletRequest(decimal Amount, string? CallbackUrl = null);
public record VerifyWalletChargeRequest(string Authority);
