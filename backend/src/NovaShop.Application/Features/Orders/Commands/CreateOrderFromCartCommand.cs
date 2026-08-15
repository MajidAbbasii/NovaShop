using FluentValidation;
using MediatR;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Common.Models;

namespace NovaShop.Application.Features.Orders.Commands;

public record CreateOrderFromCartCommand(
    int UserId,
    string ShippingAddress,
    string PaymentMethod,
    string ShippingMethod = "POST",
    decimal? ShippingCost = null,
    string? PickupLocation = null,
    string? PickupInstructions = null,
    string? PhoneNumber = null,
    string? IdempotencyKey = null,
    string? DiscountCode = null
) : IRequest<OrderDto>;

public class CreateOrderFromCartCommandValidator : AbstractValidator<CreateOrderFromCartCommand>
{
    private static bool OnlinePaymentEnabled => PaymentPolicy.OnlinePaymentEnabled;

    public CreateOrderFromCartCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("شناسه کاربر نامعتبر است");

        RuleFor(x => x.ShippingMethod)
            .NotEmpty().WithMessage("روش ارسال اجباری است")
            .Must(sm => sm is "POST" or "COURIER" or "PICKUP")
            .WithMessage("روش ارسال باید یکی از: POST, COURIER, PICKUP");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("آدرس تحویل اجباری است")
            .MinimumLength(10).WithMessage("آدرس تحویل باید حداقل ۱۰ کاراکتر باشد")
            .When(x => x.ShippingMethod != "PICKUP");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("روش پرداخت اجباری است")
            .Must(pm => pm is "InPerson" or "COD" or "CreditCard" or "PayPal" or "BankTransfer" or "Wallet" or "WalletAndOnline")
            .WithMessage("روش پرداخت نامعتبر است");

        // Temporary business mode: online payment disabled → only InPerson (پرداخت حضوری) is accepted.
        // When PaymentPolicy:OnlinePaymentEnabled=true, the allowed set widens automatically.
        RuleFor(x => x.PaymentMethod)
            .Must(pm => pm == "InPerson")
            .When(_ => !OnlinePaymentEnabled)
            .WithMessage("پرداخت آنلاین موقتاً غیرفعال است؛ فقط پرداخت حضوری امکان‌پذیر است");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("هزینه ارسال نمی‌تواند منفی باشد");
    }
}
