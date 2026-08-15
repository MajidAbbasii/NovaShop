using System.Collections.Concurrent;

namespace NovaShop.Application.Services;

/// <summary>
/// In-memory OTP store. Codes expire after 5 minutes; max 3 verify attempts.
/// Scoped for a single instance — fine for dev/single-node; move to Redis
/// before multi-instance deployment.
/// </summary>
public class OtpStore
{
    private record OtpEntry(string Code, DateTime ExpiresAt, int Attempts);

    private readonly ConcurrentDictionary<string, OtpEntry> _entries = new();

    public void Save(string phone, string code)
        => _entries[phone] = new OtpEntry(code, DateTime.UtcNow.AddMinutes(5), 0);

    public bool CanRequest(string phone)
    {
        Prune();
        return !_entries.ContainsKey(phone);
    }

    public bool TryVerify(string phone, string code)
    {
        if (!_entries.TryGetValue(phone, out var entry)) return false;
        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _entries.TryRemove(phone, out _);
            return false;
        }
        if (entry.Attempts >= 3)
        {
            _entries.TryRemove(phone, out _);
            return false;
        }
        if (!string.Equals(entry.Code, code, StringComparison.Ordinal))
        {
            _entries[phone] = entry with { Attempts = entry.Attempts + 1 };
            return false;
        }
        _entries.TryRemove(phone, out _);
        return true;
    }

    private void Prune()
    {
        foreach (var (phone, entry) in _entries)
            if (DateTime.UtcNow > entry.ExpiresAt)
                _entries.TryRemove(phone, out _);
    }
}