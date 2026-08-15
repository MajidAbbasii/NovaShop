using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Commands;

/// <summary>
/// Checks whether a customer exists for the given mobile number, without
/// revealing any account details. Used by the mobile-first login flow to
/// decide between OTP login (existing user) and registration (new user).
/// </summary>
public record CheckMobileCommand(string PhoneNumber) : IRequest<CheckMobileResponse>;

public record CheckMobileResponse(bool Exists);

public class CheckMobileCommandValidator : AbstractValidator<CheckMobileCommand>
{
    public CheckMobileCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
    }
}

public class CheckMobileCommandHandler : IRequestHandler<CheckMobileCommand, CheckMobileResponse>
{
    private readonly NovaShopDbContext _context;

    public CheckMobileCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<CheckMobileResponse> Handle(
        CheckMobileCommand request, CancellationToken cancellationToken)
    {
        // Existence-only lookup. No PII is returned to avoid user enumeration
        // beyond the boolean contract the caller already requires.
        var exists = await _context.Users.AnyAsync(
            u => u.PhoneNumber == request.PhoneNumber && u.IsActive,
            cancellationToken);
        return new CheckMobileResponse(exists);
    }
}