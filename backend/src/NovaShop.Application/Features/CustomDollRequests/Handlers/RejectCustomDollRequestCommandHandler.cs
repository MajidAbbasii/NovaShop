using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Commands;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class RejectCustomDollRequestCommandHandler : IRequestHandler<RejectCustomDollRequestCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notifications;

    public RejectCustomDollRequestCommandHandler(NovaShopDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<bool> Handle(RejectCustomDollRequestCommand request, CancellationToken ct)
    {
        var entity = await _context.CustomDollRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity == null) return false;

        if (entity.Status != CustomDollRequest.StatusPendingReview)
            return false;

        entity.Status = CustomDollRequest.StatusRejected;
        entity.AdminMessage = (request.AdminMessage ?? string.Empty).Trim();
        entity.ReviewedBy = request.AdminId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var message = string.IsNullOrWhiteSpace(entity.AdminMessage)
            ? "درخواست عروسک سفارشی شما بررسی شد و متأسفانه مورد تأیید قرار نگرفت."
            : $"درخواست عروسک سفارشی شما بررسی شد و متأسفانه مورد تأیید قرار نگرفت. پیام مدیر: {entity.AdminMessage}";
        await _notifications.NotifyInAppAsync(
            entity.UserId,
            "CustomDollRejected",
            "درخواست عروسک سفارشی رد شد",
            message,
            customDollRequestId: entity.Id,
            ct: ct);

        return true;
    }
}