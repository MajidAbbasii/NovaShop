using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Notifications.Dtos;

public class NotificationListResponse
{
    public List<AppNotificationDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
