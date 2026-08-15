using MediatR;

namespace NovaShop.Application.Features.Users.Commands;

public record DeleteUserCommand(int Id) : IRequest<bool>;
