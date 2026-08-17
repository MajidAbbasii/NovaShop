using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaShop.Common.Models;
using NovaShop.Domain.Auth;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Features.Auth.Commands;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovaShop.Application.Services;

public interface IJwtTokenService
{
    Task<LoginResponse> GenerateAndPersistAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> ResolveRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly NovaShopDbContext _context;
    private readonly JwtSettings _jwt;
    private readonly int _refreshTokenDays = 7;

    public JwtTokenService(NovaShopDbContext context, IOptions<JwtSettings> jwt)
    {
        _context = context;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> GenerateAndPersistAsync(User user, CancellationToken cancellationToken = default)
    {
        var expires = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("sub", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expires, signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Guid.NewGuid().ToString();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(_refreshTokenDays),
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Expires = expires,
        };
    }

    public async Task<User?> ResolveRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        var stored = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken, cancellationToken);

        if (stored == null || !stored.IsActive || stored.User == null || !stored.User.IsActive)
            return null;

        return stored.User;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken, cancellationToken);

        if (stored != null)
        {
            stored.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
