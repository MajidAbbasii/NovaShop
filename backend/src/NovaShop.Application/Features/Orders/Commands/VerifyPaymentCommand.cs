using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

/// <summary>
/// Server-side payment verification (the only thing that can mark an order Paid).
/// Called by the gateway callback endpoint, NEVER trusted from the browser.
/// </summary>
public record VerifyPaymentCommand(
    string Authority,
    int? UserId = null,
    string? IdempotencyKey = null
) : IRequest<PaymentResultDto>;

public class VerifyPaymentCommandValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentCommandValidator()
    {
        RuleFor(x => x.Authority).NotEmpty().WithMessage("شناسه پرداخت اجباری است");
    }
}
