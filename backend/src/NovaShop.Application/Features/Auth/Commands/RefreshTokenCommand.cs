using MediatR;
using NovaShop.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponse>;
