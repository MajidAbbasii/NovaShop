namespace NovaShop.Application.Services;

/// <summary>
/// Abstraction over an SMS gateway. Provider is selected via configuration
/// ("Sms:Provider": "Mock" | "Log" | provider-specific name).
/// </summary>
public interface ISmsService
{
    string ProviderName { get; }
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
