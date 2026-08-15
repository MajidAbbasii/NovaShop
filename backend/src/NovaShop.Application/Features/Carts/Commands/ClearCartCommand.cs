using MediatR;

namespace NovaShop.Application.Features.Carts.Commands;

public record ClearCartCommand(int UserId) : IRequest<bool>;
