using FluentValidation;
using MediatR;

public record RegisterResult
{
    public bool Pending { get; init; }
    public int? UserId { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
}

public record RegisterCommand(
    string Username,
    string Password,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? PostalCode,
    // Kept for backward-compatible API; ignored when OTP is disabled and never grants role.
    string? Role = null) : IRequest<RegisterResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Valid email is required");
        RuleFor(x => x.PhoneNumber)
            .Matches("^09\\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Valid phone number is required (e.g. 09123456789)");
    }
}
