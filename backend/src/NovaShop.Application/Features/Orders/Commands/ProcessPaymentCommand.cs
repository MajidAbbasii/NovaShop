using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

/// <summary>
/// Initiates payment for an order.
/// - WALLET: deducts wallet in-transaction, order becomes Paid immediately.
/// - WALLET_AND_ONLINE: deducts wallet (partial) + returns a RedirectUrl for the
///   remaining amount; order becomes Paid only after VerifyPaymentCommand succeeds.
/// - Other online methods: returns a RedirectUrl; order becomes Paid only after
///   VerifyPaymentCommand (server-side callback verification).
/// </summary>
public record ProcessPaymentCommand(
    int OrderId,
    int UserId,
    string? IdempotencyKey = null,
    string? CallbackUrl = null
) : IRequest<PaymentResultDto>;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
