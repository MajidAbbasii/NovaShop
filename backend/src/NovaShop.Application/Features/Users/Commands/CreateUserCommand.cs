using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Users.Commands;

public record CreateUserCommand(string Username, string Email) : IRequest<int>
{
    public string Username { get; init; } = Username;
    public string Email { get; init; } = Email;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Password { get; init; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
