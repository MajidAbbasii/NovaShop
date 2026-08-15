using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Orders.Commands;

public record CreateOrderCommand(int UserId, List<OrderItemCreateDto> Items) : IRequest<int>;

public class OrderItemCreateDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderCommandValidator : FluentValidation.AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.ProductId).GreaterThan(0);
            items.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
