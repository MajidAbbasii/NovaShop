using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly OtpStore _otpStore;
    private readonly ISmsService _smsService;
    private readonly AuthenticationOptions _authOptions;
    private readonly IJwtTokenService _tokenService;

    public RegisterCommandHandler(
        NovaShopDbContext context,
        IPasswordHasher passwordHasher,
        PendingRegistrationStore pendingStore,
        OtpStore otpStore,
        ISmsService smsService,
        IOptions<AuthenticationOptions> authOptions,
        IJwtTokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _pendingStore = pendingStore;
        _otpStore = otpStore;
        _smsService = smsService;
        _authOptions = authOptions.Value;
        _tokenService = tokenService;
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
    /// Phone is mandatory; empty phone causes validation failure.
    /// </summary>
    private async Task<RegisterResult> HandleDirectRegistration(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Phone is mandatory — do NOT generate a placeholder.
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("شماره موبایل الزامی است");

        if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken))
            throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است");

        // Email is optional; when omitted derive a non-colliding local address.
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
            PhoneNumber = request.PhoneNumber,
            Address = request.Address ?? string.Empty,
            City = request.City ?? string.Empty,
            PostalCode = request.PostalCode ?? string.Empty,
            Role = User.RoleCustomer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var login = await _tokenService.GenerateAndPersistAsync(user, cancellationToken);
        return new RegisterResult { Pending = false, UserId = user.Id, Token = login.AccessToken, RefreshToken = login.RefreshToken };
    }
}