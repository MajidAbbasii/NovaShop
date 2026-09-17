using MediatR;
using NovaShop.Application.Features.Banners.Commands;
using NovaShop.Application.Features.Banners.Queries;

namespace NovaShop.Api.Endpoints;

public static class BannersEndpoints
{
    public static IEndpointRouteBuilder MapBannersEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: active banners for the storefront hero slider
        app.MapGet("/api/banners", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetActiveBannersQuery());
            return Results.Ok(new { items = result });
        })
        .WithName("GetActiveBanners")
        .AllowAnonymous();

        // Admin: list all banners (any state)
        app.MapGet("/api/admin/banners", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAllBannersQuery());
            return Results.Ok(new { items = result });
        })
        .WithName("AdminGetBanners")
        .RequireAuthorization("AdminOnly");

        // Admin: create
        app.MapPost("/api/admin/banners", async (CreateBannerCommand command, IMediator mediator) =>
        {
            return await mediator.Send(command);
        })
        .WithName("CreateBanner")
        .RequireAuthorization("AdminOnly");

        // Admin: update
        app.MapPut("/api/admin/banners/{id}", async (int id, UpdateBannerCommand command, IMediator mediator) =>
        {
            command = command with { Id = id };
            return await mediator.Send(command);
        })
        .WithName("UpdateBanner")
        .RequireAuthorization("AdminOnly");

        // Admin: delete
        app.MapDelete("/api/admin/banners/{id}", async (int id, IMediator mediator) =>
        {
            return await mediator.Send(new DeleteBannerCommand(id));
        })
        .WithName("DeleteBanner")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}
