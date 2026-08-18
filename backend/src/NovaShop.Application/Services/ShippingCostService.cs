using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Services;

/// <summary>
/// Authoritative server-side shipping calculation.
/// This is the ONLY place shipping cost / free-shipping is computed.
/// The client never supplies a monetary amount for shipping.
///
/// Rates are sourced from the DB-backed <see cref="ShippingSetting"/> singleton
/// (admin-managed). On first access the row is seeded from <see cref="ShippingPolicy"/>
/// static defaults; the static snapshot is refreshed from the DB row so any code
/// reading ShippingPolicy stays consistent after an admin update.
/// </summary>
public interface IShippingCostService
{
    /// <summary>
    /// Validate a shipping-method string coming from the client.
    /// Returns a canonical method or null when invalid.
    /// </summary>
    string? NormalizeMethod(string? method);

    /// <summary>
    /// Calculate the shipping cost for a given method + taxable subtotal.
    /// Throws if the method is invalid.
    /// </summary>
    Task<ShippingCostResult> CalculateAsync(decimal taxableSubtotal, string shippingMethod, CancellationToken ct = default);

    /// <summary>Synchronous overload (uses cached/seed defaults).</summary>
    ShippingCostResult Calculate(decimal taxableSubtotal, string shippingMethod);
}

public record ShippingCostResult(
    string ShippingMethod,
    decimal ShippingCost,
    bool IsFreeShipping,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal GrandTotal
)
{
    public decimal GrandTotal { get; } = Subtotal - DiscountAmount + ShippingCost;
}

public class ShippingCostService : IShippingCostService
{
    private readonly IShippingSettingsRepository? _settings;

    /// <summary>
    /// DB-backed ctor (preferred). Shipping prices come from the admin-managed
    /// <see cref="ShippingSetting"/> row. The static <see cref="ShippingPolicy"/>
    /// acts only as the seed for that row on first run.
    /// </summary>
    public ShippingCostService(IShippingSettingsRepository settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Legacy ctor kept for unit tests that assert the static-policy math.
    /// Uses <see cref="ShippingPolicy"/> directly (no DB).
    /// </summary>
    public ShippingCostService()
    {
        _settings = null;
    }

    public string? NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return null;
        return method.Trim().ToUpperInvariant() switch
        {
            Order.ShippingPost => Order.ShippingPost,
            Order.ShippingCourier => Order.ShippingCourier,
            Order.ShippingPickup => Order.ShippingPickup,
            _ => null
        };
    }

    public ShippingCostResult Calculate(decimal taxableSubtotal, string shippingMethod)
        => CalculateAsync(taxableSubtotal, shippingMethod).GetAwaiter().GetResult();

    public async Task<ShippingCostResult> CalculateAsync(decimal taxableSubtotal, string shippingMethod, CancellationToken ct = default)
    {
        var method = NormalizeMethod(shippingMethod)
                     ?? throw new InvalidOperationException(
                         $"روش ارسال نامعتبر است: {shippingMethod}");

        // When no repository is supplied (legacy/unit-test path) read the static
        // ShippingPolicy snapshot directly; the DB-backed path seeds that snapshot
        // on first run so values stay consistent.
        var cfg = _settings is not null
            ? await _settings.GetOrSeedAsync(ct)
            : new ShippingSetting
            {
                CourierPrice = ShippingPolicy.CourierPrice,
                PostPrice = ShippingPolicy.PostPrice,
                PostFreeShippingThreshold = ShippingPolicy.PostFreeShippingThreshold,
                PickupPrice = ShippingPolicy.PickupPrice,
            };

        // Keep the static snapshot in sync so other readers (e.g. gateway docs)
        // reflect the current DB-backed values.
        ShippingPolicy.Apply(new ShippingOptions
        {
            CourierPrice = cfg.CourierPrice,
            PickupPrice = cfg.PickupPrice,
            Post = new PostShippingOptions
            {
                Price = cfg.PostPrice,
                FreeShippingThreshold = cfg.PostFreeShippingThreshold,
            },
        });

        var cost = method switch
        {
            Order.ShippingPickup => cfg.PickupPrice,
            Order.ShippingCourier => cfg.CourierPrice,
            Order.ShippingPost => taxableSubtotal >= cfg.PostFreeShippingThreshold
                ? 0m
                : cfg.PostPrice,
            _ => throw new InvalidOperationException(
                $"روش ارسال پشتیبانی نمی‌شود: {shippingMethod}")
        };

        var isFree = cost == 0m;

        return new ShippingCostResult(
            ShippingMethod: method,
            ShippingCost: cost,
            IsFreeShipping: isFree,
            Subtotal: taxableSubtotal,
            DiscountAmount: 0m,
            GrandTotal: taxableSubtotal + cost);
    }
}
