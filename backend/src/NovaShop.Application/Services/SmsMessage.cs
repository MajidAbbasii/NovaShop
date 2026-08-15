namespace NovaShop.Application.Services;

/// <summary>
/// A single SMS message to send.
/// </summary>
public record SmsMessage(
    string ToPhoneNumber,
    string Body,
    string? TemplateId = null,
    IReadOnlyDictionary<string, string>? Variables = null);

/// <summary>
/// Result of an SMS send attempt.
/// </summary>
public record SmsSendResult(bool Success, string? ProviderMessageId = null, string? Error = null);
