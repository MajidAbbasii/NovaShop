using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Users.Commands;

public record UpdateUserCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public string? Password { get; init; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Username).MinimumLength(3).When(x => x.Username != null);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email != null);
    }
}
