using MediatR;
using NovaShop.Application.Features.Notifications.Dtos;

namespace NovaShop.Application.Features.Notifications.Queries;

public record GetUnreadNotificationCountQuery(int UserId) : IRequest<UnreadCountResponse>;
