using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Commands;

public record ResendRegistrationCommand(string PhoneNumber) : IRequest<bool>;

public class ResendRegistrationCommandValidator : AbstractValidator<ResendRegistrationCommand>
{
    public ResendRegistrationCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
    }
}

public class ResendRegistrationCommandHandler : IRequestHandler<ResendRegistrationCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly OtpStore _otpStore;
    private readonly ISmsService _smsService;

    public ResendRegistrationCommandHandler(
        NovaShopDbContext context,
        PendingRegistrationStore pendingStore,
        OtpStore otpStore,
        ISmsService smsService)
    {
        _context = context;
        _pendingStore = pendingStore;
        _otpStore = otpStore;
        _smsService = smsService;
    }

    public async Task<bool> Handle(ResendRegistrationCommand request, CancellationToken cancellationToken)
    {
        // User must not already exist; pending registration must be in flight.
        var userExists = await _context.Users.AnyAsync(
            u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (userExists)
            throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است");

        var pending = _pendingStore.TryGet(request.PhoneNumber);
        if (!pending)
            throw new InvalidOperationException("ابتدا ثبت‌نام را شروع کنید");

        // Force-overwrite previous code so resend works immediately.
        var code = Random.Shared.Next(100000, 999999).ToString();
        _otpStore.Save(request.PhoneNumber, code);
        await _smsService.SendAsync(new SmsMessage(
            request.PhoneNumber,
            $"کد تایید ثبت‌نام شما در نوواشاپ: {code}"));

        return true;
    }
}
