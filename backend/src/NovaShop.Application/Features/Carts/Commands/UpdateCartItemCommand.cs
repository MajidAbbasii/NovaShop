using MediatR;

namespace NovaShop.Application.Features.Carts.Commands;

public record UpdateCartItemCommand(int UserId, int CartItemId, int Quantity) : IRequest<bool>;
