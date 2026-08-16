namespace NovaShop.Common.Models;

/// <summary>
/// Strongly-typed binding for the "Shipping" configuration section.
/// This is the SINGLE authoritative source of shipping rates / thresholds.
/// The frontend must never own these values — they are read only by
/// <see cref="ShippingPolicy"/> at startup and used by
/// <see cref="NovaShop.Application.Services.ShippingCostService"/>.
/// </summary>
public class ShippingOptions
{
    public const string SectionName = "Shipping";

    public PostShippingOptions Post { get; set; } = new();
    public decimal CourierPrice { get; set; } = 129_000m;
    public decimal PickupPrice { get; set; }
}

public class PostShippingOptions
{
    public decimal Price { get; set; } = 59_900m;
    public decimal FreeShippingThreshold { get; set; } = 500_000m;
}

/// <summary>
/// Process-wide snapshot of shipping rates/threshold, loaded once at startup
/// from the "Shipping" config section (like <see cref="PaymentPolicy"/>).
/// Read-only after startup so every order calculation uses identical figures.
/// </summary>
public static class ShippingPolicy
{
    public static decimal PostPrice { get; private set; } = 59_900m;
    public static decimal PostFreeShippingThreshold { get; private set; } = 500_000m;
    public static decimal CourierPrice { get; private set; } = 129_000m;
    public static decimal PickupPrice { get; private set; }

    public static void Apply(ShippingOptions options)
    {
        PostPrice = options.Post.Price;
        PostFreeShippingThreshold = options.Post.FreeShippingThreshold;
        CourierPrice = options.CourierPrice;
        PickupPrice = options.PickupPrice;
    }
}
