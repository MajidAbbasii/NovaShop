using MediatR;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Features.Orders.Dtos;
using Microsoft.EntityFrameworkCore;
using NovaShop.Common.Models;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        // Get my wallet + transactions
        app.MapGet("/api/wallet", async (
            IMediator mediator,
            HttpContext httpContext,
            NovaShopDbContext context,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            if (!PaymentPolicy.WalletEnabled)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            var userId = GetUserId(httpContext);
            if (userId == null) return Results.Unauthorized();

            var wallet = await context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == userId.Value);

            if (wallet == null)
            {
                wallet = new NovaShop.Domain.Entities.Wallet { UserId = userId.Value, Balance = 0m };
                context.Wallets.Add(wallet);
                await context.SaveChangesAsync();
            }

            var dto = new WalletDto
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt,
                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(pageSize)
                    .Select(t => new WalletTransactionDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        BalanceBefore = t.BalanceBefore,
                        BalanceAfter = t.BalanceAfter,
                        Type = t.Type,
                        Description = t.Description,
                        Reference = t.Reference,
                        OrderId = t.OrderId,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt
                    }).ToList()
            };

            return Results.Ok(dto);
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
                return Results.StatusCode(StatusCodes.Status403Forbidden);
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