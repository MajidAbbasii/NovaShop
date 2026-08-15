using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Auth.Commands;

public record RegisterResult
{
    public bool Pending { get; init; }
}

public record RegisterCommand(string Username, string PhoneNumber, string Password) : IRequest<RegisterResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^09\d{9}$").WithMessage("Valid phone number is required (e.g. 09123456789)");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}

public record VerifyRegistrationCommand(string PhoneNumber, string Code) : IRequest<LoginResponse>;

public class VerifyRegistrationCommandValidator : AbstractValidator<VerifyRegistrationCommand>
{
    public VerifyRegistrationCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^09\d{9}$").WithMessage("Valid phone number is required (e.g. 09123456789)");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required")
            .Length(6).WithMessage("Code must be 6 digits");
    }
}
