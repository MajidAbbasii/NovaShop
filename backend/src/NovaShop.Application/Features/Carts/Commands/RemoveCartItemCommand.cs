using MediatR;

namespace NovaShop.Application.Features.Carts.Commands;

public record RemoveCartItemCommand(int UserId, int CartItemId) : IRequest<bool>;
