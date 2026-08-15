namespace NovaShop.Application.Services;

/// <summary>Configuration for the SMS subsystem.</summary>
public class SmsOptions
{
    /// <summary>Which provider to use: Mock, Log, or a real gateway (e.g. Kavenegar).</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Store name shown in message templates.</summary>
    public string StoreName { get; set; } = "نوواشاپ";

    // Provider-specific settings (unused by Mock/Log providers).
    public string ApiKey { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request timeout (seconds) for the real SMS gateway HTTP call.</summary>
    public int TimeoutSeconds { get; set; } = 15;
}
