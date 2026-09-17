using MediatR;
using NovaShop.Application.Features.Notifications.Dtos;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Features.Notifications.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Notifications.Handlers;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationListResponse>
{
    private readonly NovaShopDbContext _context;

    public GetMyNotificationsQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponse> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var query = _context.AppNotifications
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking()
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new Orders.Dtos.AppNotificationDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                CustomDollRequestId = n.CustomDollRequestId,
                Type = n.Type,
                Channel = n.Channel,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);

        return new NotificationListResponse
        {
            Items = items,
            Total = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
