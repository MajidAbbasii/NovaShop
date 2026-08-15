using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Commands;

public record VerifyWalletChargeCommand(
    string Authority,
    string? IdempotencyKey = null
) : IRequest<WalletChargeResultDto>;

public class VerifyWalletChargeCommandValidator : AbstractValidator<VerifyWalletChargeCommand>
{
    public VerifyWalletChargeCommandValidator()
    {
        RuleFor(x => x.Authority).NotEmpty().WithMessage("شناسه پرداخت اجباری است");
    }
}