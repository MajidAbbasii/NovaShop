using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly OtpStore _otpStore;
    private readonly ISmsService _smsService;

    public RegisterCommandHandler(
        NovaShopDbContext context,
        IPasswordHasher passwordHasher,
        PendingRegistrationStore pendingStore,
        OtpStore otpStore,
        ISmsService smsService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _pendingStore = pendingStore;
        _otpStore = otpStore;
        _smsService = smsService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phoneExists = await _context.Users.AnyAsync(
            u => u.PhoneNumber == request.PhoneNumber || u.Username == request.Username, cancellationToken);
        if (phoneExists)
            throw new InvalidOperationException("این شماره موبایل یا نام کاربری قبلاً ثبت شده است");

        if (!_otpStore.CanRequest(request.PhoneNumber))
            throw new InvalidOperationException("کد قبلی هنوز معتبر است؛ لطفاً کمی صبر کنید");

        // Stage 1: save pending registration + send OTP. User created only after verification.
        _pendingStore.Save(request.PhoneNumber, request.Username, _passwordHasher.Hash(request.Password));

        var code = Random.Shared.Next(100000, 999999).ToString();
        _otpStore.Save(request.PhoneNumber, code);
        await _smsService.SendAsync(new SmsMessage(
            request.PhoneNumber,
            $"کد تایید ثبت‌نام شما در نوواشاپ: {code}"));

        return new RegisterResult { Pending = true };
    }
}
