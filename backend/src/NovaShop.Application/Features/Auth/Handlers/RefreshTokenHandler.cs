using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Common.Models;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly JwtSettings _jwt;

    public RefreshTokenCommandHandler(IOptions<JwtSettings> jwt)
    {
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // چک Refresh Token (بعداً از دیتابیس)
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token");

        // تولید Token جدید
        var newAccessToken = GenerateAccessToken("admin"); // بعداً از UserId
        var newRefreshToken = GenerateRefreshToken();

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            Expires = DateTime.Now.AddHours(8)
        };
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString(); // منحصر به فرد
    }

    private string GenerateAccessToken(string username)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("Permission", "Product.Read"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
