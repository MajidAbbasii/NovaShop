using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Discounts.Commands;

public record ApplyDiscountToOrderCommand(
    int UserId,
    int OrderId,
    string DiscountCode
) : IRequest<OrderDto>;

public class ApplyDiscountToOrderCommandValidator : AbstractValidator<ApplyDiscountToOrderCommand>
{
    public ApplyDiscountToOrderCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.DiscountCode).NotEmpty().MaximumLength(50);
    }
}
