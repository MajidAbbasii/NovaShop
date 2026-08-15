using Microsoft.Extensions.Logging;
using NovaShop.Domain.Services;

namespace NovaShop.Infrastructure.Services;

/// <summary>
/// Development mock gateway supporting the full redirect + server-side
/// verification flow, so the complete payment lifecycle can be tested
/// without a real Iranian PSP.
///
/// Flow:
///   InitiatePaymentAsync → returns a RedirectUrl pointing at the app's own
///   mock gateway page (MOCK-GATEWAY/...). That page lets the tester choose
///   success / fail / cancel, then it calls back into /api/payments/verify.
///   VerifyPaymentAsync → checks the stored session and returns success only
///   when the session was marked PAID and the amount matches.
///
/// A real provider (Zarinpal, Saman, etc.) implements the same interface.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> InitiatePaymentAsync(
        string paymentMethod,
        decimal amount,
        string currency,
        string callbackUrl,
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        var authority = $"AUTH-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

        // Persist pending payment our verify step will consult.
        MockPaymentStore.Instance.Create(new MockPaymentSession
        {
            Authority = authority,
            Amount = amount,
            Currency = currency,
            PaymentMethod = paymentMethod,
            CallbackUrl = callbackUrl,
            OrderReference = orderReference,
            Status = "PENDING"
        });

        var redirectUrl = $"/mock-gateway/{authority}";
        _logger.LogInformation(
            "MockPayment: initiated method={Method} amount={Amount} {Currency} authority={Authority}",
            paymentMethod, amount, currency, authority);

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            RedirectUrl = redirectUrl,
            Authority = authority,
            TransactionId = $"TXN-{Guid.NewGuid():N}"[..20]
        });
    }

    public Task<PaymentVerificationResult> VerifyPaymentAsync(
        string paymentMethod,
        string authority,
        decimal expectedAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var session = MockPaymentStore.Instance.Get(authority);
        if (session == null)
        {
            _logger.LogWarning("MockPayment: verify for unknown authority {Authority}", authority);
            return Task.FromResult(new PaymentVerificationResult
            {
                Success = false,
                FailureReason = "Authority نامعتبر است"
            });
        }

        if (session.Status != "PAID")
        {
            _logger.LogWarning("MockPayment: verify for non-paid authority {Authority} status={Status}",
                authority, session.Status);
            return Task.FromResult(new PaymentVerificationResult
            {
                Success = false,
                FailureReason = session.Status == "CANCELLED"
                    ? "پرداخت توسط کاربر لغو شد"
                    : "پرداخت انجام نشده است"
            });
        }

        if (session.Amount != expectedAmount)
        {
            _logger.LogWarning("MockPayment: amount mismatch authority={Authority} expected={Expected} actual={Actual}",
                authority, expectedAmount, session.Amount);
            return Task.FromResult(new PaymentVerificationResult
            {
                Success = false,
                FailureReason = "مبلغ پرداختی با مبلغ سفارش مطابقت ندارد"
            });
        }

        MockPaymentStore.Instance.MarkVerified(authority);
        _logger.LogInformation("MockPayment: VERIFIED authority={Authority} amount={Amount}",
            authority, session.Amount);

        return Task.FromResult(new PaymentVerificationResult
        {
            Success = true,
            TransactionId = $"TXN-{Guid.NewGuid():N}"[..20],
            VerifiedAmount = session.Amount
        });
    }
}

/// <summary>
/// In-memory store for mock payment sessions (authority → session).
/// Simulates the payment provider's server-side state. A real provider
/// would query its own API in VerifyPaymentAsync instead.
/// </summary>
public sealed class MockPaymentStore
{
    public static readonly MockPaymentStore Instance = new();
    private readonly Dictionary<string, MockPaymentSession> _sessions = new();
    private readonly Lock _lock = new();

    public void Create(MockPaymentSession session)
    {
        lock (_lock) _sessions[session.Authority] = session;
    }

    public MockPaymentSession? Get(string authority)
    {
        lock (_lock) return _sessions.TryGetValue(authority, out var s) ? s : null;
    }

    public void MarkPaid(string authority, string? note = null)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(authority, out var s))
            {
                s.Status = "PAID";
                s.Note = note;
            }
        }
    }

    public void MarkVerified(string authority)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(authority, out var s)) s.Status = "VERIFIED";
        }
    }

    public void MarkCancelled(string authority)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(authority, out var s)) s.Status = "CANCELLED";
        }
    }

    public IEnumerable<MockPaymentSession> All()
    {
        lock (_lock) return _sessions.Values.ToList();
    }
}

public class MockPaymentSession
{
    public string Authority { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string OrderReference { get; init; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? Note { get; set; }
}
