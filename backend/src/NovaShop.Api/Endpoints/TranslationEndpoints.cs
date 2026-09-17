using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Application.Services;

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

        return app;
    }
}
