using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

public record UpdateOrderStatusCommand(
    int OrderId,
    string Status,
    string? Note = null,
    int? ChangedByUserId = null,
    string? ChangedByRole = null) : IRequest<OrderDto>;