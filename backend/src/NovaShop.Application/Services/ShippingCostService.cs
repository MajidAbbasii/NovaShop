using NovaShop.Common.Models;
using NovaShop.Domain.Entities;

namespace NovaShop.Application.Services;

/// <summary>
/// Authoritative server-side shipping calculation.
/// This is the ONLY place shipping cost / free-shipping is computed.
/// The client never supplies a monetary amount for shipping.
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
    {
        var method = NormalizeMethod(shippingMethod)
                     ?? throw new InvalidOperationException(
                         $"روش ارسال نامعتبر است: {shippingMethod}");

        var cost = method switch
        {
            Order.ShippingPickup => ShippingPolicy.PickupPrice,
            Order.ShippingCourier => ShippingPolicy.CourierPrice,
            Order.ShippingPost => taxableSubtotal >= ShippingPolicy.PostFreeShippingThreshold
                ? 0m
                : ShippingPolicy.PostPrice,
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
