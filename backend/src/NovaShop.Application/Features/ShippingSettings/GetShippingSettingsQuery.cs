using MediatR;
using NovaShop.Application.Features.ShippingSettings;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.ShippingSettings.Queries;

/// <summary>Customer-facing: list available shipping methods with their
/// current DB-backed prices. No price is ever trusted from the client.</summary>
public record GetShippingMethodsQuery : IRequest<ShippingMethodsDto>;

public class GetShippingMethodsQueryHandler
    : IRequestHandler<GetShippingMethodsQuery, ShippingMethodsDto>
{
    private readonly IShippingSettingsRepository _settings;

    public GetShippingMethodsQueryHandler(IShippingSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<ShippingMethodsDto> Handle(GetShippingMethodsQuery request, CancellationToken ct)
    {
        var cfg = await _settings.GetOrSeedAsync(ct);

        var methods = new List<ShippingMethodDto>
        {
            new(Order.ShippingCourier, "checkout.method.courier", cfg.CourierPrice, cfg.CourierPrice == 0m),
            new(Order.ShippingPost, "checkout.method.post", cfg.PostPrice, cfg.PostPrice == 0m),
            new(Order.ShippingPickup, "checkout.method.pickup", cfg.PickupPrice, cfg.PickupPrice == 0m),
        };

        return new ShippingMethodsDto(methods, cfg.PostFreeShippingThreshold);
    }
}

/// <summary>Admin: read current shipping settings.</summary>
public record GetShippingSettingsQuery : IRequest<ShippingSettingsDto>;

public class GetShippingSettingsQueryHandler
    : IRequestHandler<GetShippingSettingsQuery, ShippingSettingsDto>
{
    private readonly IShippingSettingsRepository _settings;

    public GetShippingSettingsQueryHandler(IShippingSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<ShippingSettingsDto> Handle(GetShippingSettingsQuery request, CancellationToken ct)
    {
        var cfg = await _settings.GetOrSeedAsync(ct);
        return new ShippingSettingsDto(
            cfg.CourierPrice, cfg.PostPrice, cfg.PostFreeShippingThreshold, cfg.PickupPrice);
    }
}
