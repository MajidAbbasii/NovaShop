using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NovaShop.Application.Services;

/// <summary>
/// Real Iranian SMS gateway integration for Kavenegar
/// (https://kavenegar.com — REST docs at https://kavenegar.com/rest.html).
///
/// Endpoint:  POST {BaseUrl}/v1/{ApiKey}/sms/send.json
/// Form body: receptor=&amp;sender=&amp;message=
/// Success:   HTTP 200 and return.status == 200, entries[0].messageid returned.
///
/// This is the ONLY place that talks to the Kavenegar HTTP API. It reuses the existing
/// ISmsService contract and SmsSendResult, so callers (NotificationService, the
/// Hangfire retry job) need no changes. No secrets are logged.
/// </summary>
public class KavenegarSmsService : ISmsService
{
    private const string DefaultBaseUrl = "https://api.kavenegar.com";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmsOptions _options;
    private readonly ILogger<KavenegarSmsService> _logger;

    public KavenegarSmsService(
        IHttpClientFactory httpClientFactory,
        IOptions<SmsOptions> options,
        ILogger<KavenegarSmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Kavenegar";

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        // Fail fast on missing configuration — never throw into the caller.
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Kavenegar send skipped: ApiKey not configured.");
            return new SmsSendResult(false, Error: "Kavenegar ApiKey is not configured.");
        }

        if (!IsValidIranianMobile(message.ToPhoneNumber))
        {
            _logger.LogWarning("Kavenegar send rejected: invalid recipient {Masked}.", Mask(message.ToPhoneNumber));
            return new SmsSendResult(false, Error: "Invalid recipient phone number.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? DefaultBaseUrl : _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1/{_options.ApiKey}/sms/send.json";

        var formFields = new Dictionary<string, string>
        {
            ["receptor"] = message.ToPhoneNumber,
            ["message"] = message.Body
        };
        if (!string.IsNullOrWhiteSpace(_options.SenderNumber))
            formFields["sender"] = _options.SenderNumber;

        var body = new FormUrlEncodedContent(formFields);

        using var client = _httpClientFactory.CreateClient(nameof(KavenegarSmsService));
        client.Timeout = RequestTimeout;
        var masked = Mask(message.ToPhoneNumber);

        try
        {
            _logger.LogInformation(
                "Kavenegar send -> {Masked} (sender={Sender}, len={Len})",
                masked, string.IsNullOrWhiteSpace(_options.SenderNumber) ? "<default>" : _options.SenderNumber, message.Body.Length);

            using var response = await client.PostAsync(url, body, cancellationToken);

            // Kavenegar returns HTTP 200 with return.status != 200 for logical errors,
            // and 4xx for auth/validation problems. Read the body either way.
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Kavenegar HTTP {StatusCode} for {Masked}. Body={Body}",
                    (int)response.StatusCode, masked, Truncate(raw, 300));
                return new SmsSendResult(false,
                    Error: $"Provider returned HTTP {(int)response.StatusCode}");
            }

            var parsed = TryParse(raw, out var apiReturn, out var entries);
            if (!parsed)
            {
                _logger.LogError("Kavenegar: unparsable response for {Masked}: {Body}", masked, Truncate(raw, 300));
                return new SmsSendResult(false, Error: "Unparsable provider response.");
            }

            if (apiReturn.Status != 200)
            {
                _logger.LogError(
                    "Kavenegar API error {Status} ({Message}) for {Masked}.",
                    apiReturn.Status, apiReturn.Message, masked);
                return new SmsSendResult(false, Error: $"Provider error {apiReturn.Status}: {apiReturn.Message}");
            }

            var messageId = entries.Count > 0 ? entries[0].MessageId.ToString(CultureInfo.InvariantCulture) : null;
            _logger.LogInformation(
                "Kavenegar sent OK -> {Masked}. messageId={MessageId}", masked, messageId);
            return new SmsSendResult(true, ProviderMessageId: messageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Kavenegar send timed out for {Masked}.", masked);
            return new SmsSendResult(false, Error: "Provider request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Kavenegar network error for {Masked}.", masked);
            return new SmsSendResult(false, Error: "Network error contacting SMS provider.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kavenegar unexpected error for {Masked}.", masked);
            return new SmsSendResult(false, Error: "Unexpected SMS provider error.");
        }
    }

    private static bool TryParse(string raw, out KavenegarReturn apiReturn, out List<KavenegarEntry> entries)
    {
        apiReturn = new KavenegarReturn();
        entries = new List<KavenegarEntry>();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("return", out var ret))
            {
                apiReturn.Status = ret.TryGetProperty("status", out var s) ? s.GetInt32() : 0;
                apiReturn.Message = ret.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
            }
            if (root.TryGetProperty("entries", out var ent) && ent.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in ent.EnumerateArray())
                {
                    var entry = new KavenegarEntry();
                    if (e.TryGetProperty("messageid", out var mid) && mid.TryGetInt64(out var v)) entry.MessageId = v;
                    entries.Add(entry);
                }
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsValidIranianMobile(string? phone)
        => phone is not null && IranianMobileRegex().IsMatch(phone);

    private static System.Text.RegularExpressions.Regex IranianMobileRegex()
        => new System.Text.RegularExpressions.Regex(@"^09\d{9}$");

    private static string Mask(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4)
            return "****";
        return phone[..2] + new string('*', phone.Length - 4) + phone[^2..];
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";

    private sealed record KavenegarReturn
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private sealed record KavenegarEntry
    {
        public long MessageId { get; set; }
    }
}
