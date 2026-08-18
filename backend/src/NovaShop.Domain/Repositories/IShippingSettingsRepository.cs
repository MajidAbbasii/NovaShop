using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

/// <summary>
/// Reads/writes the singleton <see cref="ShippingSetting"/> row that holds
/// admin-managed shipping rates. The row is seeded from <see cref="ShippingPolicy"/>
/// static defaults when it does not yet exist.
/// </summary>
public interface IShippingSettingsRepository
{
    /// <summary>Returns the current settings, seeding a default row on first access.</summary>
    Task<ShippingSetting> GetOrSeedAsync(CancellationToken ct = default);

    Task UpdateAsync(ShippingSetting settings, CancellationToken ct = default);
}
