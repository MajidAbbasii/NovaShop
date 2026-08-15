using System.Collections.Concurrent;

namespace NovaShop.Application.Services;

/// <summary>
/// In-memory store of registrations awaiting SMS OTP confirmation.
/// Entries expire after 10 minutes.
/// </summary>
public class PendingRegistrationStore
{
    private record PendingEntry(string Username, string PasswordHash, string PhoneNumber, DateTime ExpiresAt);

    private readonly ConcurrentDictionary<string, PendingEntry> _entries = new();

    public void Save(string phone, string username, string passwordHash)
        => _entries[phone] = new PendingEntry(username, passwordHash, phone, DateTime.UtcNow.AddMinutes(10));

    public bool TryGet(string phone)
    {
        Prune();
        return _entries.ContainsKey(phone);
    }

    public bool TryTake(string phone, out string username, out string passwordHash)
    {
        username = passwordHash = string.Empty;
        Prune();
        if (!_entries.TryRemove(phone, out var entry)) return false;
        username = entry.Username;
        passwordHash = entry.PasswordHash;
        return true;
    }

    private void Prune()
    {
        foreach (var (phone, entry) in _entries)
            if (DateTime.UtcNow > entry.ExpiresAt)
                _entries.TryRemove(phone, out _);
    }
}