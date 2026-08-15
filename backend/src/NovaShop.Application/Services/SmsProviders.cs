using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NovaShop.Application.Services;

/// <summary>
/// Development-only provider: "sends" by logging the message. Always succeeds,
/// so the order workflow can be tested without a real SMS gateway.
/// </summary>
public class LogSmsService(ILogger<LogSmsService> logger) : ISmsService
{
    public string ProviderName => "Log";

    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[SMS:Log] To={To} Body={Body} Template={Template}",
            message.ToPhoneNumber, message.Body, message.TemplateId);
        return Task.FromResult(new SmsSendResult(true, ProviderMessageId: $"LOG-{Guid.NewGuid():N}"[..20]));
    }
}

/// <summary>
/// Development-only provider: no-op. Useful when even log noise is unwanted.
/// </summary>
public class MockSmsService : ISmsService
{
    public string ProviderName => "Mock";

    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(new SmsSendResult(true, ProviderMessageId: $"MOCK-{Guid.NewGuid():N}"[..20]));
}

/// <summary>
/// Picks the SMS provider based on configuration. Add real providers here
/// (e.g. KavenegarSmsService) without touching callers.
/// </summary>
public static class SmsServiceFactory
{
    public static ISmsService Create(IServiceProvider serviceProvider, string provider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        return provider switch
        {
            "Log" => new LogSmsService(loggerFactory.CreateLogger<LogSmsService>()),
            "Kavenegar" => new KavenegarSmsService(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<SmsOptions>>(),
                loggerFactory.CreateLogger<KavenegarSmsService>()),
            _ => new MockSmsService()
        };
    }
}
