using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Queries;

public record GetOrdersQuery : IRequest<PagedResult<OrderDto>>
{
    public int? UserId { get; init; }
    public string? Status { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record GetOrderQuery(int Id) : IRequest<OrderDto>;

/// <summary>Inventory ledger entries (admin).</summary>
public record GetInventoryTransactionsQuery(
    int? ProductId = null,
    int? OrderId = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<InventoryTransactionDto>>;

/// <summary>SMS notification log (admin).</summary>
public record GetSmsNotificationsQuery(
    int? OrderId = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<SmsNotificationDto>>;
