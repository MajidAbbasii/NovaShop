using MediatR;
using NovaShop.Application.Features.Auth.Commands;

namespace NovaShop.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(new { Token = response.AccessToken });
        })
        .WithName("Login")
        .AllowAnonymous();

        app.MapPost("/api/auth/refresh", async (RefreshTokenCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(response);
        })
        .AllowAnonymous();

        app.MapPost("/api/auth/register", async (RegisterCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(response);
        })
        .AllowAnonymous();

        app.MapPost("/api/auth/register/resend", async (ResendRegistrationCommand command, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(command);
                return Results.Ok(new { message = "کد تایید مجدداً ارسال شد" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ResendRegistration")
        .AllowAnonymous();

        app.MapPost("/api/auth/register/verify", async (VerifyRegistrationCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(new { Token = response.AccessToken });
        })
        .WithName("VerifyRegistration")
        .AllowAnonymous();

        app.MapPost("/api/auth/logout", () => Results.Ok("Logged out"))
            .RequireAuthorization();

        app.MapPost("/api/auth/check-mobile", async (CheckMobileCommand command, IMediator mediator) =>
        {
            // Existence-only: returns { exists: bool }. No PII exposed.
            // Same response shape whether or not the number exists, to limit
            // user-enumeration surface.
            var response = await mediator.Send(command);
            return Results.Ok(new { exists = response.Exists });
        })
        .WithName("CheckMobile")
        .AllowAnonymous();

        app.MapPost("/api/auth/otp/request", async (RequestOtpCommand command, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(command);
                return Results.Ok(new { message = "کد ورود ارسال شد" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 429);
            }
        })
        .WithName("RequestOtp")
        .AllowAnonymous();

        app.MapPost("/api/auth/otp/verify", async (VerifyOtpCommand command, IMediator mediator) =>
        {
            var response = await mediator.Send(command);
            return Results.Ok(new { Token = response.AccessToken });
        })
        .WithName("VerifyOtp")
        .AllowAnonymous();

        return app;
    }
}
