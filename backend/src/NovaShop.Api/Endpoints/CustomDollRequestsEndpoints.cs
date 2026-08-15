using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class CustomDollRequestsEndpoints
{
    public static IEndpointRouteBuilder MapCustomDollRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        // Customer: create request
        app.MapPost("/api/custom-doll-requests", async (
            CreateCustomDollRequestRequest req,
            ClaimsPrincipal user,
            NovaShopDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.ImageUrl))
                return Results.BadRequest(new { message = "تصویر الزامی است" });

            var request = new CustomDollRequest
            {
                UserId = userId.Value,
                ImageUrl = req.ImageUrl.Trim(),
                Description = (req.Description ?? string.Empty).Trim(),
                Status = CustomDollRequest.StatusPendingReview,
                Currency = CustomDollRequest.CurrencyToman
            };

            db.CustomDollRequests.Add(request);
            await db.SaveChangesAsync();
            return Results.Created($"/api/custom-doll-requests/{request.Id}", request.Id);
        })
        .WithName("CreateCustomDollRequest")
        .RequireAuthorization();

        // Customer: my requests
        app.MapGet("/api/custom-doll-requests", async (
            ClaimsPrincipal user,
            NovaShopDbContext db,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var query = db.CustomDollRequests
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => ToDto(r))
                .ToListAsync();

            return Results.Ok(new { items, total, pageNumber, pageSize });
        })
        .WithName("GetMyCustomDollRequests")
        .RequireAuthorization();

        // Customer: my requests (explicit segment — must precede /{id})
        app.MapGet("/api/custom-doll-requests/my", async (
            ClaimsPrincipal user,
            NovaShopDbContext db,
            int pageNumber = 1,
            int pageSize = 50) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var query = db.CustomDollRequests
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => ToDto(r))
                .ToListAsync();

            return Results.Ok(new { items, total, pageNumber, pageSize });
        })
        .WithName("GetMyCustomDollRequestsAlt")
        .RequireAuthorization();

        // Customer: request detail (own only)
        app.MapGet("/api/custom-doll-requests/{id}", async (
            int id,
            ClaimsPrincipal user,
            NovaShopDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var request = await db.CustomDollRequests
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value);
            if (request == null) return Results.NotFound();

            return Results.Ok(ToDto(request));
        })
        .WithName("GetMyCustomDollRequest")
        .RequireAuthorization();

        // Customer: accept approved price (final confirmation before crafting)
        app.MapPost("/api/custom-doll-requests/{id}/accept", async (
            int id,
            ClaimsPrincipal user,
            NovaShopDbContext db,
            INotificationService notifications) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var request = await db.CustomDollRequests
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value);
            if (request == null) return Results.NotFound();

            if (request.Status != CustomDollRequest.StatusApproved)
                return Results.BadRequest(new { message = "فقط درخواست‌های تأییدشده قابل پذیرش نهایی هستند" });

            request.Status = CustomDollRequest.StatusCustomerAccepted;
            request.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var admin = await db.Users
                .Where(u => u.Id == request.ReviewedBy)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (admin != 0)
            {
                await notifications.NotifyInAppAsync(admin, "CustomDollAccepted", "پذیرش نهایی درخواست",
                    $"مشتری درخواست #{request.Id} را با قیمت {request.Price:N0} تومان پذیرفت. فرآیند ساخت را آغاز کنید.",
                    customDollRequestId: request.Id);
            }

            return Results.Ok();
        })
        .WithName("AcceptCustomDollRequest")
        .RequireAuthorization();

        // Admin: list all
        app.MapGet("/api/admin/custom-doll-requests", async (
            NovaShopDbContext db,
            int pageNumber = 1,
            int pageSize = 50,
            string? status = null) =>
        {
            var query = db.CustomDollRequests.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminCustomDollRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    CustomerUsername = r.User.Username,
                    CustomerPhone = r.User.PhoneNumber,
                    ImageUrl = r.ImageUrl,
                    Description = r.Description,
                    Status = r.Status,
                    Price = r.Price,
                    Currency = r.Currency,
                    AdminMessage = r.AdminMessage,
                    ReviewedBy = r.ReviewedBy,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    ReviewedAt = r.ReviewedAt
                })
                .ToListAsync();

            return Results.Ok(new { items, total, pageNumber, pageSize });
        })
        .WithName("AdminGetCustomDollRequests")
        .RequireAuthorization("AdminOnly");

        // Admin: detail
        app.MapGet("/api/admin/custom-doll-requests/{id}", async (
            int id,
            NovaShopDbContext db) =>
        {
            var request = await db.CustomDollRequests
                .Where(r => r.Id == id)
                .Select(r => new AdminCustomDollRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    CustomerUsername = r.User.Username,
                    CustomerPhone = r.User.PhoneNumber,
                    CustomerEmail = r.User.Email,
                    ImageUrl = r.ImageUrl,
                    Description = r.Description,
                    Status = r.Status,
                    Price = r.Price,
                    Currency = r.Currency,
                    AdminMessage = r.AdminMessage,
                    ReviewedBy = r.ReviewedBy,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    ReviewedAt = r.ReviewedAt
                })
                .FirstOrDefaultAsync();
            if (request == null) return Results.NotFound();

            return Results.Ok(request);
        })
        .WithName("AdminGetCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        // Admin: approve (sets price + message)
        app.MapPost("/api/admin/custom-doll-requests/{id}/approve", async (
            int id,
            ApproveCustomDollRequestRequest req,
            ClaimsPrincipal user,
            NovaShopDbContext db,
            INotificationService notifications) =>
        {
            var request = await db.CustomDollRequests.FindAsync(id);
            if (request == null) return Results.NotFound();

            if (request.Status != CustomDollRequest.StatusPendingReview)
                return Results.BadRequest(new { message = "فقط درخواست‌های در انتظار بررسی قابل تأیید هستند" });

            if (req == null || !req.Price.HasValue || req.Price.Value <= 0)
                return Results.BadRequest(new { message = "تعیین قیمت برای تأیید درخواست الزامی است" });

            var adminId = GetUserId(user);
            request.Status = CustomDollRequest.StatusApproved;
            request.Price = req.Price.Value;
            request.Currency = CustomDollRequest.CurrencyToman;
            request.AdminMessage = (req.AdminMessage ?? string.Empty).Trim();
            request.ReviewedBy = adminId;
            request.ReviewedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var priceText = $"{request.Price:N0}";
            await notifications.NotifyInAppAsync(request.UserId, "CustomDollApproved", "درخواست عروسک سفارشی تأیید شد",
                $"درخواست عروسک سفارشی شما تأیید شد. عروسک شما با هزینه {priceText} تومان تهیه خواهد شد.", customDollRequestId: request.Id);

            return Results.Ok();
        })
        .WithName("ApproveCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        // Admin: reject
        app.MapPost("/api/admin/custom-doll-requests/{id}/reject", async (
            int id,
            RejectCustomDollRequestRequest req,
            ClaimsPrincipal user,
            NovaShopDbContext db,
            INotificationService notifications) =>
        {
            var request = await db.CustomDollRequests.FindAsync(id);
            if (request == null) return Results.NotFound();

            if (req == null)
                return Results.BadRequest(new { message = "درخواست نامعتبر است" });

            if (request.Status != CustomDollRequest.StatusPendingReview)
                return Results.BadRequest(new { message = "فقط درخواست‌های در انتظار بررسی قابل رد شدن هستند" });

            var adminId = GetUserId(user);
            request.Status = CustomDollRequest.StatusRejected;
            request.AdminMessage = (req.AdminMessage ?? string.Empty).Trim();
            request.ReviewedBy = adminId;
            request.ReviewedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var message = string.IsNullOrWhiteSpace(request.AdminMessage)
                ? "درخواست عروسک سفارشی شما بررسی شد و متأسفانه مورد تأیید قرار نگرفت."
                : $"درخواست عروسک سفارشی شما بررسی شد و متأسفانه مورد تأیید قرار نگرفت. پیام مدیر: {request.AdminMessage}";
            await notifications.NotifyInAppAsync(request.UserId, "CustomDollRejected", "درخواست عروسک سفارشی رد شد", message,
                customDollRequestId: request.Id);

            return Results.Ok();
        })
        .WithName("RejectCustomDollRequest")
        .RequireAuthorization("AdminOnly");

        return app;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (claim == null || !int.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }

    private static CustomDollRequestDto ToDto(CustomDollRequest r) => new()
    {
        Id = r.Id,
        ImageUrl = r.ImageUrl,
        Description = r.Description,
        Status = r.Status,
        Price = r.Price,
        Currency = r.Currency,
        AdminMessage = r.AdminMessage,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        ReviewedAt = r.ReviewedAt
    };
}

public class CustomDollRequestDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? AdminMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class AdminCustomDollRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerUsername { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? AdminMessage { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public record CreateCustomDollRequestRequest(string ImageUrl, string? Description);
public record ApproveCustomDollRequestRequest(decimal? Price, string? AdminMessage);
public record RejectCustomDollRequestRequest(string? AdminMessage);
