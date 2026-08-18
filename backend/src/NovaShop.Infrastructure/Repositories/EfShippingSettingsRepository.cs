using Microsoft.EntityFrameworkCore;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfShippingSettingsRepository : IShippingSettingsRepository
{
    private readonly NovaShopDbContext _context;

    public EfShippingSettingsRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingSetting> GetOrSeedAsync(CancellationToken ct = default)
    {
        var settings = await _context.ShippingSettings
            .FirstOrDefaultAsync(s => s.Id == ShippingSetting.SingletonId, ct);

        if (settings != null) return settings;

        // Seed the singleton row from the static ShippingPolicy defaults so the
        // system has sane values on first run. Admin can change them any time.
        settings = new ShippingSetting
        {
            Id = ShippingSetting.SingletonId,
            CourierPrice = ShippingPolicy.CourierPrice,
            PostPrice = ShippingPolicy.PostPrice,
            PostFreeShippingThreshold = ShippingPolicy.PostFreeShippingThreshold,
            PickupPrice = ShippingPolicy.PickupPrice,
        };

        _context.ShippingSettings.Add(settings);
        await _context.SaveChangesAsync(ct);
        return settings;
    }

    public async Task UpdateAsync(ShippingSetting settings, CancellationToken ct = default)
    {
        var existing = await _context.ShippingSettings
            .FirstOrDefaultAsync(s => s.Id == ShippingSetting.SingletonId, ct);

        if (existing == null)
        {
            settings.Id = ShippingSetting.SingletonId;
            _context.ShippingSettings.Add(settings);
        }
        else
        {
            existing.CourierPrice = settings.CourierPrice;
            existing.PostPrice = settings.PostPrice;
            existing.PostFreeShippingThreshold = settings.PostFreeShippingThreshold;
            existing.PickupPrice = settings.PickupPrice;
        }

        await _context.SaveChangesAsync(ct);
    }
}
