using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Products.Commands;

public record DeleteProductCommand : IRequest<bool>
{
    public int Id { get; init; }
}

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
