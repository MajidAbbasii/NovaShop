using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

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
    private readonly IJwtTokenService _tokenService;

    public VerifyOtpCommandHandler(NovaShopDbContext context, OtpStore otpStore, IJwtTokenService tokenService)
    {
        _context = context;
        _otpStore = otpStore;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (!_otpStore.TryVerify(request.PhoneNumber, request.Code))
            throw new UnauthorizedAccessException("کد وارد شده نامعتبر یا منقضی شده است");

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == request.PhoneNumber && u.IsActive, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("کاربری با این شماره موبایل یافت نشد");

        return await _tokenService.GenerateAndPersistAsync(user, cancellationToken);
    }
}