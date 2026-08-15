namespace NovaShop.Application.Features.Orders.Dtos;

public class PaymentResultDto
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    /// <summary>Set when the customer must be redirected to the payment gateway.</summary>
    public string? RedirectUrl { get; set; }
    /// <summary>Gateway authority/token used for verification.</summary>
    public string? Authority { get; set; }
    /// <summary>Amount the customer must pay online (wallet partial payment).</summary>
    public decimal? OnlineAmount { get; set; }
    /// <summary>Amount deducted from the wallet (wallet payment).</summary>
    public decimal? WalletAmount { get; set; }
    /// <summary>Remaining wallet balance after a wallet payment.</summary>
    public decimal? WalletBalance { get; set; }
}

public class WalletChargeResultDto
{
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public bool Success { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Authority { get; set; }
    public string? FailureReason { get; set; }
}
