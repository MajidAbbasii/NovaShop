namespace NovaShop.Domain.Entities;

/// <summary>
/// UI translation record. One row per Key + Locale. This table is the single
/// authoritative source of UI translations for the application.
/// </summary>
public class Translation
{
    public int Id { get; set; }

    /// <summary>Dot-path key, e.g. "common.save", "order.status.paid".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Canonical locale identifier: "fa", "en", "ar".</summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>Translated value for the given Key + Locale.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Optional logical grouping, e.g. "common", "admin", "product".</summary>
    public string? Namespace { get; set; }

    /// <summary>Human description of where/how the key is used (admin aid).</summary>
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}
