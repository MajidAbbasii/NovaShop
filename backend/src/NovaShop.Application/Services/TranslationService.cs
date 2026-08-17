using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Caching;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Services;

public interface ITranslationService
{
    /// <summary>All active translations for a locale as a flat key->value map (cached).</summary>
    Task<Dictionary<string, string>> GetLocaleMapAsync(string locale, CancellationToken ct = default);

    Task<AdminTranslationPage> SearchAsync(TranslationFilter filter, CancellationToken ct = default);

    Task<TranslationDetail?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<TranslationDetail> CreateAsync(CreateTranslationRequest request, string? actor, CancellationToken ct = default);

    Task<TranslationDetail?> UpdateAsync(int id, UpdateTranslationRequest request, string? actor, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Report keys that are missing for one or more of the supported locales.</summary>
    Task<MissingReport> GetMissingReportAsync(CancellationToken ct = default);
}

public class TranslationService : ITranslationService
{
    internal const string CachePrefix = "translations:loc:";
    private static readonly string[] Supported = { "fa", "en", "ar" };

    private readonly NovaShopDbContext _db;
    private readonly ICacheService _cache;

    public TranslationService(NovaShopDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Dictionary<string, string>> GetLocaleMapAsync(string locale, CancellationToken ct = default)
    {
        var key = CachePrefix + locale;
        var cached = await _cache.GetAsync<Dictionary<string, string>>(key);
        if (cached is not null) return cached;

        var map = await _db.Translations
            .Where(t => t.Locale == locale && t.IsActive)
            .ToDictionaryAsync(t => t.Key, t => t.Value, ct);

        await _cache.SetAsync(key, map);
        return map;
    }

    public async Task<AdminTranslationPage> SearchAsync(TranslationFilter f, CancellationToken ct = default)
    {
        var q = _db.Translations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Locale))
            q = q.Where(t => t.Locale == f.Locale);
        if (!string.IsNullOrWhiteSpace(f.Namespace))
            q = q.Where(t => t.Namespace == f.Namespace);
        if (!string.IsNullOrWhiteSpace(f.Key))
            q = q.Where(t => t.Key.Contains(f.Key));
        if (!string.IsNullOrWhiteSpace(f.Search))
            q = q.Where(t => t.Key.Contains(f.Search) || t.Value.Contains(f.Search) || (t.Description != null && t.Description.Contains(f.Search)));
        if (f.OnlyMissing)
            q = q.Where(t => !t.IsActive || t.Value == "");

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .Skip((f.PageNumber - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(t => new TranslationDetail
            {
                Id = t.Id,
                Key = t.Key,
                Locale = t.Locale,
                Value = t.Value,
                Namespace = t.Namespace,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CreatedBy = t.CreatedBy,
                UpdatedBy = t.UpdatedBy,
            })
            .ToListAsync(ct);

        return new AdminTranslationPage
        {
            Items = items,
            TotalCount = total,
            PageNumber = f.PageNumber,
            PageSize = f.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)f.PageSize),
        };
    }

    public async Task<TranslationDetail?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.Translations.FindAsync(new object[] { id }, ct);
        return t is null ? null : ToDetail(t);
    }

    public async Task<TranslationDetail> CreateAsync(CreateTranslationRequest req, string? actor, CancellationToken ct = default)
    {
        // A "create" may carry multiple locales for the same key. Upsert each locale row
        // so re-creating an existing Key+Locale is idempotent and concurrency-safe.
        Translation? primary = null;
        foreach (var v in req.Values)
        {
            var existing = await _db.Translations
                .FirstOrDefaultAsync(x => x.Key == req.Key && x.Locale == v.Locale, ct);
            if (existing is null)
            {
                existing = new Translation { Key = req.Key, Locale = v.Locale };
                _db.Translations.Add(existing);
            }
            existing.Value = v.Value;
            existing.Namespace = req.Namespace;
            existing.Description = req.Description;
            existing.IsActive = true;
            existing.CreatedBy ??= actor;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = actor;
            primary ??= existing;
        }
        await _db.SaveChangesAsync(ct);
        await InvalidateLocaleAsync(req.Values.Select(v => v.Locale));
        return ToDetail(primary!);
    }

    public async Task<TranslationDetail?> UpdateAsync(int id, UpdateTranslationRequest req, string? actor, CancellationToken ct = default)
    {
        var t = await _db.Translations.FindAsync(new object[] { id }, ct);
        if (t is null) return null;

        if (req.Value is not null) t.Value = req.Value;
        if (req.Namespace is not null) t.Namespace = req.Namespace;
        if (req.Description is not null) t.Description = req.Description;
        if (req.IsActive is not null) t.IsActive = req.IsActive.Value;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = actor;

        await _db.SaveChangesAsync(ct);
        await InvalidateLocaleAsync(new[] { t.Locale });
        return ToDetail(t);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.Translations.FindAsync(new object[] { id }, ct);
        if (t is null) return false;
        _db.Translations.Remove(t);
        await _db.SaveChangesAsync(ct);
        await InvalidateLocaleAsync(new[] { t.Locale });
        return true;
    }

    public async Task<MissingReport> GetMissingReportAsync(CancellationToken ct = default)
    {
        // For every key that exists in ANY locale, find which supported locales lack an
        // active, non-empty row.
        var rows = await _db.Translations
            .Where(t => t.IsActive && t.Value != "")
            .GroupBy(t => t.Key)
            .Select(g => new { Key = g.Key, Locales = g.Select(x => x.Locale).ToList() })
            .ToListAsync(ct);

        var missing = rows
            .Where(r => r.Locales.Count < Supported.Length)
            .Select(r => new MissingKey(
                r.Key,
                Supported.Where(l => !r.Locales.Contains(l)).ToList()))
            .ToList();

        return new MissingReport
        {
            SupportedLocales = Supported.ToList(),
            TotalKeys = rows.Count,
            MissingCount = missing.Count,
            Missing = missing,
        };
    }

    private async Task InvalidateLocaleAsync(IEnumerable<string> locales)
    {
        foreach (var l in locales.Distinct())
            await _cache.RemoveAsync(CachePrefix + l);
    }

    private static TranslationDetail ToDetail(Translation t) => new()
    {
        Id = t.Id,
        Key = t.Key,
        Locale = t.Locale,
        Value = t.Value,
        Namespace = t.Namespace,
        Description = t.Description,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CreatedBy = t.CreatedBy,
        UpdatedBy = t.UpdatedBy,
    };
}

// ---- DTOs ----
public record TranslationDetail
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public record CreateTranslationRequest(
    string Key,
    string? Namespace,
    string? Description,
    List<LocaleValue> Values);

public record LocaleValue(string Locale, string Value);

public record UpdateTranslationRequest(
    string? Value = null,
    string? Namespace = null,
    string? Description = null,
    bool? IsActive = null);

public record TranslationFilter(
    int PageNumber = 1,
    int PageSize = 20,
    string? Locale = null,
    string? Namespace = null,
    string? Key = null,
    string? Search = null,
    bool OnlyMissing = false);

public record AdminTranslationPage
{
    public List<TranslationDetail> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public record MissingKey(string Key, List<string> MissingLocales);
public record MissingReport
{
    public List<string> SupportedLocales { get; set; } = new();
    public int TotalKeys { get; set; }
    public int MissingCount { get; set; }
    public List<MissingKey> Missing { get; set; } = new();
}
