namespace NovaShop.Domain.Services;

public class PaymentResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
    /// <summary>Set when the gateway requires a redirect (online payment).</summary>
    public string? RedirectUrl { get; init; }
    /// <summary>Authority/redirect token issued by the gateway (e.g. Zarinpal authority).</summary>
    public string? Authority { get; init; }
    /// <summary>True when the payment still needs server-side verification.</summary>
    public bool RequiresVerification => !string.IsNullOrEmpty(Authority) || !string.IsNullOrEmpty(RedirectUrl);
}

public class PaymentVerificationResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
    public decimal? VerifiedAmount { get; init; }
}

public interface IPaymentGateway
{
    /// <summary>
    /// Initiate a payment. For online methods returns a RedirectUrl/Authority
    /// that the frontend must navigate to; the payment is only final after
    /// VerifyPaymentAsync succeeds (server-side callback verification).
    /// </summary>
    Task<PaymentResult> InitiatePaymentAsync(
        string paymentMethod,
        decimal amount,
        string currency,
        string callbackUrl,
        string orderReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a previously initiated payment. MUST be called server-side;
    /// the backend never trusts a browser redirect alone.
    /// </summary>
    Task<PaymentVerificationResult> VerifyPaymentAsync(
        string paymentMethod,
        string authority,
        decimal expectedAmount,
        string currency,
        CancellationToken cancellationToken = default);
}
