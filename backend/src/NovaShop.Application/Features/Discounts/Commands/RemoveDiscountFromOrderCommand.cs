using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Discounts.Commands;

public record RemoveDiscountFromOrderCommand(int UserId, int OrderId) : IRequest<OrderDto>;

public class RemoveDiscountFromOrderCommandValidator : AbstractValidator<RemoveDiscountFromOrderCommand>
{
    public RemoveDiscountFromOrderCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.OrderId).GreaterThan(0);
    }
}
