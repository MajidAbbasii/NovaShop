using MediatR;
using Microsoft.EntityFrameworkCore;
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

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly OtpStore _otpStore;
    private readonly ISmsService _smsService;
    private readonly AuthenticationOptions _authOptions;
    private readonly JwtSettings _jwt;

    public RegisterCommandHandler(
        NovaShopDbContext context,
        IPasswordHasher passwordHasher,
        PendingRegistrationStore pendingStore,
        OtpStore otpStore,
        ISmsService smsService,
        IOptions<AuthenticationOptions> authOptions,
        IOptions<JwtSettings> jwt)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _pendingStore = pendingStore;
        _otpStore = otpStore;
        _smsService = smsService;
        _authOptions = authOptions.Value;
        _jwt = jwt.Value;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Public registration is always Customer. The Role field (if supplied) is dropped.
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new InvalidOperationException("این نام کاربری قبلاً استفاده شده است");

        if (_authOptions.OtpEnabled)
            return await HandleOtpRegistration(request, cancellationToken);

        return await HandleDirectRegistration(request, cancellationToken);
    }

    /// <summary>
    /// Original mobile-first flow: stage pending registration + send SMS OTP.
    /// User row is created only after OTP verification (VerifyRegistrationCommandHandler).
    /// </summary>
    private async Task<RegisterResult> HandleOtpRegistration(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("شماره موبایل الزامی است");
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken))
            throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است");

        if (!_otpStore.CanRequest(request.PhoneNumber))
            throw new InvalidOperationException("کد قبلی هنوز معتبر است؛ لطفاً کمی صبر کنید");

        _pendingStore.Save(request.PhoneNumber, request.Username, _passwordHasher.Hash(request.Password));
        var code = Random.Shared.Next(100000, 999999).ToString();
        _otpStore.Save(request.PhoneNumber, code);
        await _smsService.SendAsync(new SmsMessage(
            request.PhoneNumber,
            $"کد تایید ثبت‌نام شما در نوواشاپ: {code}"));

        return new RegisterResult { Pending = true };
    }

    /// <summary>
    /// Direct registration (OTP disabled): create the user immediately and issue a JWT
    /// so the client can sign them in without an extra verification step.
    /// Phone is optional; when omitted a unique placeholder keeps the phone unique index valid.
    /// </summary>
    private async Task<RegisterResult> HandleDirectRegistration(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? $"unset_{Guid.NewGuid():N}"[..18]
            : request.PhoneNumber;

        // Email is optional; when omitted derive a non-colliding local address (OTP paths do this too).
        var email = string.IsNullOrWhiteSpace(request.Email)
            ? $"{request.Username}@novashop.local"
            : request.Email;

        var user = new User
        {
            Username = request.Username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            PhoneNumber = phone,
            Address = request.Address ?? string.Empty,
            City = request.City ?? string.Empty,
            PostalCode = request.PostalCode ?? string.Empty,
            Role = User.RoleCustomer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var token = IssueToken(user);
        return new RegisterResult { Pending = false, UserId = user.Id, Token = token };
    }

    private string IssueToken(User user)
    {
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
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
