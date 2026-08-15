namespace NovaShop.Common.Models;

/// <summary>
/// Process-wide snapshot of the temporary payment/wallet business mode.
/// Loaded once at startup from "PaymentPolicy" config by the API host.
/// Kept in NovaShop.Common so Application-layer logic (validators, handlers)
/// can read the flags without an extra DI hop. Read-only after startup.
/// </summary>
public static class PaymentPolicy
{
    public static bool OnlinePaymentEnabled { get; set; }
    public static bool WalletEnabled { get; set; }
    public static bool InPersonPaymentEnabled { get; set; } = true;
    public static bool OrderCreationEnabled { get; set; } = true;

    public static void Apply(PaymentPolicyOptions options)
    {
        OnlinePaymentEnabled = options.OnlinePaymentEnabled;
        WalletEnabled = options.WalletEnabled;
        InPersonPaymentEnabled = options.InPersonPaymentEnabled;
        OrderCreationEnabled = options.OrderCreationEnabled;
    }
}

public class PaymentPolicyOptions
{
    public bool OnlinePaymentEnabled { get; set; }
    public bool WalletEnabled { get; set; }
    public bool InPersonPaymentEnabled { get; set; } = true;
    public bool OrderCreationEnabled { get; set; } = true;
}
