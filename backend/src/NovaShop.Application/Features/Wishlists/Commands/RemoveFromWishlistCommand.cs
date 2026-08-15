using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Wishlists.Commands;

public record RemoveFromWishlistCommand(int UserId, int ProductId) : IRequest<bool>;

public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand>
{
    public RemoveFromWishlistCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
