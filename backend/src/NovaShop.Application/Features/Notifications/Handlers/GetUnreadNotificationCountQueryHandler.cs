using MediatR;
using NovaShop.Application.Features.Notifications.Dtos;
using NovaShop.Application.Features.Notifications.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Notifications.Handlers;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, UnreadCountResponse>
{
    private readonly NovaShopDbContext _context;

    public GetUnreadNotificationCountQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<UnreadCountResponse> Handle(GetUnreadNotificationCountQuery request, CancellationToken ct)
    {
        var count = await _context.AppNotifications
            .CountAsync(n => n.UserId == request.UserId && !n.IsRead, ct);
        return new UnreadCountResponse { Count = count };
    }
}
