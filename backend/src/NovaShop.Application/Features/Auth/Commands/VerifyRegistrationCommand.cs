using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Auth.Commands;

public record VerifyRegistrationCommand(string PhoneNumber, string Code) : IRequest<LoginResponse>;

public class VerifyRegistrationCommandValidator : AbstractValidator<VerifyRegistrationCommand>
{
    public VerifyRegistrationCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
            .Matches("^09\\d{9}$").WithMessage("Valid phone number is required (e.g. 09123456789)");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required")
            .Length(6).WithMessage("Code must be 6 digits");
    }
}
