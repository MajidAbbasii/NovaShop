namespace NovaShop.Application.Features.Orders.Dtos;

/// <summary>
/// Server-calculated order quote. The frontend displays these figures but
/// MUST NOT trust them at order-creation time — the handler recalculates
/// everything independently from trusted DB data.
/// </summary>
public class OrderQuoteDto
{
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountCode { get; set; }
    public decimal ShippingCost { get; set; }
    public bool IsFreeShipping { get; set; }
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
}
