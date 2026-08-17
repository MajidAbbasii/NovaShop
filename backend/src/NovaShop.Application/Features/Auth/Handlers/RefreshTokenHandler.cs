using MediatR;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Application.Services;

namespace NovaShop.Application.Features.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IJwtTokenService _tokenService;

    public RefreshTokenCommandHandler(IJwtTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Resolve the real user behind the persisted refresh token. A missing,
        // expired, revoked, or orphaned token is rejected — no role is ever assumed.
        var user = await _tokenService.ResolveRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Rotate: revoke the used token, then issue a fresh access + refresh pair.
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return await _tokenService.GenerateAndPersistAsync(user, cancellationToken);
    }
}
