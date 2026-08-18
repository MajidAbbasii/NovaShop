namespace NovaShop.Domain.Entities;

/// <summary>
/// Singleton (Id = 1) store for admin-managed shipping rates.
/// This is the authoritative DB-backed source of shipping prices.
/// The frontend may never supply a price; the backend always reads from here
/// (seeded from ShippingPolicy static defaults when no row exists yet).
/// Historical order ShippingCost is a snapshot baked into each Order row, so
/// changing these values never affects past orders.
/// </summary>
public class ShippingSetting
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Courier (پیک موتوری) flat price in Toman.</summary>
    public decimal CourierPrice { get; set; }

    /// <summary>Post (پست پیشتاز) flat price in Toman.</summary>
    public decimal PostPrice { get; set; }

    /// <summary>Orders at/above this subtotal (Toman) get free post shipping.</summary>
    public decimal PostFreeShippingThreshold { get; set; }

    /// <summary>Pickup (تحویل حضوری) price — normally 0.</summary>
    public decimal PickupPrice { get; set; }
}
