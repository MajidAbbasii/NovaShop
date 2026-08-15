using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Commands;

public record RequestOtpCommand(string PhoneNumber) : IRequest<bool>;

public class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
    }
}

public class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly OtpStore _otpStore;
    private readonly ISmsService _smsService;

    public RequestOtpCommandHandler(NovaShopDbContext context, OtpStore otpStore, ISmsService smsService)
    {
        _context = context;
        _otpStore = otpStore;
        _smsService = smsService;
    }

    public async Task<bool> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // Only existing, active users can request a code.
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == request.PhoneNumber && u.IsActive, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("کاربری با این شماره موبایل یافت نشد");

        if (!_otpStore.CanRequest(request.PhoneNumber))
            throw new InvalidOperationException("کد قبلی هنوز معتبر است؛ لطفاً کمی صبر کنید");

        var code = Random.Shared.Next(100000, 999999).ToString();
        _otpStore.Save(request.PhoneNumber, code);

        await _smsService.SendAsync(new SmsMessage(
            request.PhoneNumber,
            $"کد ورود شما به نوواشاپ: {code}"));
        return true;
    }
}