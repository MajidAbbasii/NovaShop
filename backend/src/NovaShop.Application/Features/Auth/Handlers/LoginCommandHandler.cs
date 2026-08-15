using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovaShop.Application.Features.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwt;

    public LoginCommandHandler(NovaShopDbContext context, IPasswordHasher passwordHasher, IOptions<JwtSettings> jwt)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Empty credentials fail fast (same message as any other failure).
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            throw new UnauthorizedAccessException("Invalid username or password");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid username or password");

        // Password verification is mandatory — a token is issued ONLY after a
        // successful hash comparison.
        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

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
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = Guid.NewGuid().ToString(),
            Expires = DateTime.UtcNow.AddHours(8),
        };
    }
}