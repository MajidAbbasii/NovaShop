using MediatR;
using NovaShop.Application.Features.Notifications.Commands;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Notifications.Handlers;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly NovaShopDbContext _context;

    public MarkAllNotificationsReadCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await _context.AppNotifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return unread.Count;
    }
}