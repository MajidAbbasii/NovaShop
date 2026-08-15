using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

/// <summary>
/// Refunds a paid order to the customer wallet. Idempotent — refuses to
/// refund the same order twice (Order.RefundProcessed guard + unique wallet
/// transaction reference).
/// </summary>
public record RefundOrderCommand(
    int OrderId,
    int UserId,
    string? Reason = null,
    bool FullRefund = true,
    decimal? Amount = null
) : IRequest<PaymentResultDto>;

public class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Amount)
            .GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("مبلغ بازگشت باید بزرگ‌تر از صفر باشد");
    }
}
