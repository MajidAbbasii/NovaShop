using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Commands;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class AcceptCustomDollRequestCommandHandler : IRequestHandler<AcceptCustomDollRequestCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly INotificationService _notifications;

    public AcceptCustomDollRequestCommandHandler(NovaShopDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<bool> Handle(AcceptCustomDollRequestCommand request, CancellationToken ct)
    {
        var entity = await _context.CustomDollRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.UserId == request.UserId, ct);
        if (entity == null) return false;

        if (entity.Status != CustomDollRequest.StatusApproved)
            return false;

        entity.Status = CustomDollRequest.StatusCustomerAccepted;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var admin = await _context.Users
            .Where(u => u.Id == entity.ReviewedBy)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (admin != 0)
        {
            await _notifications.NotifyInAppAsync(
                admin,
                "CustomDollAccepted",
                "پذیرش نهایی درخواست",
                $"مشتری درخواست #{entity.Id} را با قیمت {entity.Price:N0} تومان پذیرفت. فرآیند ساخت را آغاز کنید.",
                customDollRequestId: entity.Id,
                ct: ct);
        }

        return true;
    }
}