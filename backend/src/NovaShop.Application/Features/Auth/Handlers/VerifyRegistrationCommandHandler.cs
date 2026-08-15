using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovaShop.Application.Features.Auth.Handlers;

public class VerifyRegistrationCommandHandler : IRequestHandler<VerifyRegistrationCommand, LoginResponse>
{
    private readonly NovaShopDbContext _context;
    private readonly OtpStore _otpStore;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly JwtSettings _jwt;

    public VerifyRegistrationCommandHandler(NovaShopDbContext context, OtpStore otpStore, PendingRegistrationStore pendingStore, IOptions<JwtSettings> jwt)
    {
        _context = context;
        _otpStore = otpStore;
        _pendingStore = pendingStore;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> Handle(VerifyRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (!_otpStore.TryVerify(request.PhoneNumber, request.Code))
            throw new UnauthorizedAccessException("کد وارد شده نامعتبر یا منقضی شده است");

        if (!_pendingStore.TryTake(request.PhoneNumber, out var username, out var passwordHash))
            throw new UnauthorizedAccessException("ابتدا فرم ثبت‌نام را تکمیل کنید");

        var user = new User
        {
            Username = username,
            Email = $"{request.PhoneNumber}@novashop.local",
            PasswordHash = passwordHash,
            Role = User.RoleCustomer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FirstName = "",
            LastName = "",
            PhoneNumber = request.PhoneNumber,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

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
