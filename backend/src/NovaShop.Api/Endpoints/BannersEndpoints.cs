using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

public static class BannersEndpoints
{
    public static IEndpointRouteBuilder MapBannersEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: active banners for the storefront hero slider
        app.MapGet("/api/banners", async (NovaShopDbContext db) =>
        {
            var banners = await db.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Id)
                .Select(b => new BannerDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Subtitle = b.Subtitle,
                    ImageUrl = b.ImageUrl,
                    LinkUrl = b.LinkUrl,
                    SortOrder = b.SortOrder
                })
                .ToListAsync();

            return Results.Ok(new { items = banners });
        })
        .WithName("GetActiveBanners")
        .AllowAnonymous();

        // Admin: list all banners (any state)
        app.MapGet("/api/admin/banners", async (NovaShopDbContext db) =>
        {
            var banners = await db.Banners
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Id)
                .Select(b => new BannerDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Subtitle = b.Subtitle,
                    ImageUrl = b.ImageUrl,
                    LinkUrl = b.LinkUrl,
                    IsActive = b.IsActive,
                    SortOrder = b.SortOrder,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(new { items = banners });
        })
        .WithName("AdminGetBanners")
        .RequireAuthorization("AdminOnly");

        // Admin: create
        app.MapPost("/api/admin/banners", async (BannerUpsertRequest req, NovaShopDbContext db) =>
        {
            var banner = new Banner
            {
                Title = req.Title.Trim(),
                Subtitle = (req.Subtitle ?? string.Empty).Trim(),
                ImageUrl = (req.ImageUrl ?? string.Empty).Trim(),
                LinkUrl = (req.LinkUrl ?? string.Empty).Trim(),
                IsActive = req.IsActive,
                SortOrder = req.SortOrder
            };

            db.Banners.Add(banner);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/banners/{banner.Id}", banner.Id);
        })
        .WithName("CreateBanner")
        .RequireAuthorization("AdminOnly");

        // Admin: update
        app.MapPut("/api/admin/banners/{id}", async (int id, BannerUpsertRequest req, NovaShopDbContext db) =>
        {
            var banner = await db.Banners.FindAsync(id);
            if (banner == null) return Results.NotFound();

            banner.Title = req.Title.Trim();
            banner.Subtitle = (req.Subtitle ?? string.Empty).Trim();
            banner.ImageUrl = (req.ImageUrl ?? string.Empty).Trim();
            banner.LinkUrl = (req.LinkUrl ?? string.Empty).Trim();
            banner.IsActive = req.IsActive;
            banner.SortOrder = req.SortOrder;
            banner.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateBanner")
        .RequireAuthorization("AdminOnly");

        // Admin: delete
        app.MapDelete("/api/admin/banners/{id}", async (int id, NovaShopDbContext db) =>
        {
            var banner = await db.Banners.FindAsync(id);
            if (banner == null) return Results.NotFound();

            db.Banners.Remove(banner);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteBanner")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}

public class BannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record BannerUpsertRequest(
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? LinkUrl,
    bool IsActive = true,
    int SortOrder = 0);