using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Wishlists.Commands;

public record AddToWishlistCommand : IRequest<bool>
{
    public int UserId { get; init; }
    public int ProductId { get; init; }
    public string? Note { get; init; }
}

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
