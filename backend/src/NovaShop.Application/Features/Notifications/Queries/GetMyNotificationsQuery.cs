using MediatR;
using NovaShop.Application.Features.Notifications.Dtos;

namespace NovaShop.Application.Features.Notifications.Queries;

public record GetMyNotificationsQuery(int UserId, int PageNumber = 1, int PageSize = 50) : IRequest<NotificationListResponse>;
