using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Auth.Commands;

public record LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
}

public record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}