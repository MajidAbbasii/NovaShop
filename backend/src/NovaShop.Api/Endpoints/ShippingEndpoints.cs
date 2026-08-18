using FluentValidation;
using MediatR;
using NovaShop.Application.Features.ShippingSettings;
using NovaShop.Application.Features.ShippingSettings.Commands;
using NovaShop.Application.Features.ShippingSettings.Queries;

namespace NovaShop.Api.Endpoints;

public static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        // Customer: available shipping methods + current DB-backed rates.
        // The client may never supply a price; this is display-only.
        app.MapGet("/api/shipping-methods", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetShippingMethodsQuery());
            return Results.Ok(result);
        })
        .WithName("GetShippingMethods")
        .RequireAuthorization();

        // Admin: read current shipping settings.
        app.MapGet("/api/admin/shipping-settings", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetShippingSettingsQuery());
            return Results.Ok(result);
        })
        .WithName("GetShippingSettings")
        .RequireAuthorization("AdminOnly");

        // Admin: update shipping settings (Courier / Post / Pickup prices in Toman).
        app.MapPut("/api/admin/shipping-settings", async (
            UpdateShippingSettingsCommand command,
            IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateShippingSettings")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}
