using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovaShop.Application.Features.Auth.Commands;

public record VerifyOtpCommand(string PhoneNumber, string Code) : IRequest<LoginResponse>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد الزامی است")
            .Length(6).WithMessage("کد باید ۶ رقم باشد");
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, LoginResponse>
{
    private readonly NovaShopDbContext _context;
    private readonly OtpStore _otpStore;
    private readonly JwtSettings _jwt;

    public VerifyOtpCommandHandler(NovaShopDbContext context, OtpStore otpStore, IOptions<JwtSettings> jwt)
    {
        _context = context;
        _otpStore = otpStore;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (!_otpStore.TryVerify(request.PhoneNumber, request.Code))
            throw new UnauthorizedAccessException("کد وارد شده نامعتبر یا منقضی شده است");

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == request.PhoneNumber && u.IsActive, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("کاربری با این شماره موبایل یافت نشد");

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