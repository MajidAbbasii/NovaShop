using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Carts.Commands;

public record CreateCartCommand : IRequest<int>
{
    public CreateCartCommand(int userId)
    {
        UserId = userId;
    }

    public int UserId { get; init; }
}

public class CreateCartCommandValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId معتبر نیست");
    }
}
