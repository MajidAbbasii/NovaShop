using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Commands;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class ApproveCustomDollRequestCommandHandler : IRequestHandler<ApproveCustomDollRequestCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notifications;

    public ApproveCustomDollRequestCommandHandler(NovaShopDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<bool> Handle(ApproveCustomDollRequestCommand request, CancellationToken ct)
    {
        var entity = await _context.CustomDollRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity == null) return false;

        if (entity.Status != CustomDollRequest.StatusPendingReview)
            return false;

        entity.Status = CustomDollRequest.StatusApproved;
        entity.Price = request.Price;
        entity.Currency = CustomDollRequest.CurrencyToman;
        entity.AdminMessage = (request.AdminMessage ?? string.Empty).Trim();
        entity.ReviewedBy = request.AdminId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var priceText = $"{entity.Price:N0}";
        await _notifications.NotifyInAppAsync(
            entity.UserId,
            "CustomDollApproved",
            "درخواست عروسک سفارشی تأیید شد",
            $"درخواست عروسک سفارشی شما تأیید شد. عروسک شما با هزینه {priceText} تومان تهیه خواهد شد.",
            customDollRequestId: entity.Id,
            ct: ct);

        return true;
    }
}