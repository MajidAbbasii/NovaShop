using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Caching;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- Public, read-only, cached bulk translations ----
        app.MapGet("/api/translations", async (string locale, ITranslationService svc, CancellationToken ct) =>
        {
            if (locale != "fa" && locale != "en" && locale != "ar")
                return Results.BadRequest(new { error = "Unsupported locale" });

            var map = await svc.GetLocaleMapAsync(locale, ct);
            return Results.Ok(new { locale, translations = map });
        })
        .AllowAnonymous()
        .WithName("GetTranslations");

        // ---- Admin CRUD ----
        var admin = app.MapGroup("/api/admin/translations").RequireAuthorization("AdminOnly");

        admin.MapGet("", async ([FromQuery] int? pageNumber, [FromQuery] int? pageSize,
            [FromQuery] string? locale, [FromQuery] string? @namespace, [FromQuery] string? key,
            [FromQuery] string? search, [FromQuery] bool? onlyMissing, ITranslationService svc, CancellationToken ct) =>
        {
            var page = await svc.SearchAsync(new TranslationFilter(
                PageNumber: pageNumber ?? 1,
                PageSize: pageSize ?? 20,
                Locale: locale, Namespace: @namespace, Key: key, Search: search, OnlyMissing: onlyMissing ?? false), ct);
            return Results.Ok(page);
        })
        .WithName("AdminListTranslations");

        admin.MapGet("/missing", async (ITranslationService svc, CancellationToken ct) =>
        {
            var report = await svc.GetMissingReportAsync(ct);
            return Results.Ok(report);
        })
        .WithName("AdminMissingTranslations");

        admin.MapGet("/{id:int}", async (int id, ITranslationService svc, CancellationToken ct) =>
        {
            var item = await svc.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("AdminGetTranslation");

        admin.MapPost("", async (CreateTranslationRequest req, HttpContext ctx, ITranslationService svc, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Key))
                return Results.BadRequest(new { error = "Key is required" });
            if (req.Values.Count == 0)
                return Results.BadRequest(new { error = "At least one translation value is required" });

            var actor = ctx.User?.Identity?.Name;
            var created = await svc.CreateAsync(req, actor, ct);
            return Results.Created($"/api/admin/translations/{created.Id}", created);
        })
        .WithName("AdminCreateTranslation");

        admin.MapPut("/{id:int}", async (int id, UpdateTranslationRequest req, HttpContext ctx,
            ITranslationService svc, CancellationToken ct) =>
        {
            var actor = ctx.User?.Identity?.Name;
            var updated = await svc.UpdateAsync(id, req, actor, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("AdminUpdateTranslation");

        admin.MapDelete("/{id:int}", async (int id, ITranslationService svc, CancellationToken ct) =>
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithName("AdminDeleteTranslation");

        // ---- Temporary bulk import — REMOVE after migration is complete ----
        // Restores the full Translations table on Render from a JSON export.
        // Accepts a flat array of records with explicit IDs; skips existing Key+Locale
        // and ID conflicts; resets the PostgreSQL identity sequence after insert.
        admin.MapPost("/import", async (
            List<TranslationImportRecord> records,
            NovaShopDbContext db,
            ICacheService cache,
            CancellationToken ct) =>
        {
            if (records.Count is 0 or > 5000)
                return Results.BadRequest(new { error = "Provide 1-5000 records" });

            // Load existing Key+Locale pairs and IDs in one query
            var existing = await db.Translations
                .Select(t => new { t.Key, t.Locale, t.Id })
                .ToListAsync(ct);
            var existingKeyLocales = new HashSet<string>(existing.Select(t => $"{t.Key}|{t.Locale}"));
            var existingIds = new HashSet<int>(existing.Select(t => t.Id));

            var now = DateTime.UtcNow;
            var toAdd = new List<Translation>();
            var skipped = 0;
            var failed = 0;

            foreach (var r in records)
            {
                if (string.IsNullOrWhiteSpace(r.Key) || string.IsNullOrWhiteSpace(r.Locale) || string.IsNullOrWhiteSpace(r.Value))
                { failed++; continue; }

                if (existingKeyLocales.Contains($"{r.Key}|{r.Locale}"))
                { skipped++; continue; }

                if (r.Id > 0 && existingIds.Contains(r.Id))
                { skipped++; continue; }

                toAdd.Add(new Translation
                {
                    Id = r.Id,
                    Key = r.Key.Trim(),
                    Locale = r.Locale.Trim(),
                    Value = r.Value,
                    Namespace = r.Namespace,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt == default ? now : r.CreatedAt,
                    UpdatedAt = r.UpdatedAt == default ? now : r.UpdatedAt,
                    CreatedBy = r.CreatedBy ?? "migration",
                    UpdatedBy = r.UpdatedBy ?? "migration",
                });
            }

            if (toAdd.Count > 0)
            {
                await db.Translations.AddRangeAsync(toAdd, ct);
                await db.SaveChangesAsync(ct);

                // Reset PostgreSQL identity sequence to MAX(Id) to prevent future ID conflicts
                await db.Database.ExecuteSqlRawAsync(
                    """SELECT setval(pg_get_serial_sequence('"Translations"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Translations"), 1))""",
                    ct);
            }

            // Invalidate translation cache for all locales
            const string cachePrefix = "translations:loc:";
            await cache.RemoveAsync(cachePrefix + "fa");
            await cache.RemoveAsync(cachePrefix + "en");
            await cache.RemoveAsync(cachePrefix + "ar");

            return Results.Ok(new { received = records.Count, inserted = toAdd.Count, skipped, failed });
        })
        .WithName("AdminImportTranslations");

        return app;
    }
}

/// <summary>
/// Temporary DTO for bulk translation import. REMOVE after migration is complete.
/// </summary>
public record TranslationImportRecord
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Namespace { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
