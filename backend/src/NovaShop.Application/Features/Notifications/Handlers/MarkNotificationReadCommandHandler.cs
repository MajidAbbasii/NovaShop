using MediatR;
using NovaShop.Application.Features.Notifications.Commands;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Notifications.Handlers;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public MarkNotificationReadCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _context.AppNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, ct);
        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
