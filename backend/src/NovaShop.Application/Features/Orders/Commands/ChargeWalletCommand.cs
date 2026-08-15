using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

/// <summary>
/// Charges the customer wallet via the online gateway.
/// Balance only increases after server-side verification.
/// </summary>
public record ChargeWalletCommand(
    int UserId,
    decimal Amount,
    string? IdempotencyKey = null,
    string? CallbackUrl = null
) : IRequest<WalletChargeResultDto>;

public class ChargeWalletCommandValidator : AbstractValidator<ChargeWalletCommand>
{
    public ChargeWalletCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ شارژ باید بزرگ‌تر از صفر باشد")
            .LessThanOrEqualTo(100_000_000m).WithMessage("مبلغ شارژ بیش از حد مجاز است");
    }
}
