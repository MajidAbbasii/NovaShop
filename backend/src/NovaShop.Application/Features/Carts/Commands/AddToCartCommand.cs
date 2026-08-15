using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Carts.Commands;

public record AddToCartCommand : IRequest<bool>
{
    public AddToCartCommand(int userId, int productId, int quantity, int? productColorId = null)
    {
        UserId = userId;
        ProductId = productId;
        Quantity = quantity;
        ProductColorId = productColorId;
    }

    public int UserId { get; init; }
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public int? ProductColorId { get; init; }
}

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.ProductId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
    }
}
