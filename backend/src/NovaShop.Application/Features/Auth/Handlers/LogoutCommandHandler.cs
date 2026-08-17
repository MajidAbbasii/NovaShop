using MediatR;
using NovaShop.Application.Services;

namespace NovaShop.Application.Features.Auth.Handlers;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IJwtTokenService _tokenService;

    public LogoutCommandHandler(IJwtTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Revoke the refresh token so the stored session can no longer be used to
        // mint new access tokens. The access token remains valid until expiry, but
        // cannot be refreshed after logout.
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return true;
    }
}
