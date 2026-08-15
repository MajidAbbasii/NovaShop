using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Discounts.Commands;

public record CreateDiscountCommand : IRequest<int>
{
    public string Code { get; init; } = string.Empty;
    public string Type { get; init; } = "Percentage";
    public decimal Value { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int UsageLimit { get; init; }
    public decimal MinOrderAmount { get; init; }
    public List<int> ApplicableProductIds { get; init; } = new();
    public List<int> ApplicableCategoryIds { get; init; } = new();
    public bool IsActive { get; init; } = true;
}

public class CreateDiscountCommandValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد تخفیف اجباری است")
            .MaximumLength(50).WithMessage("کد تخفیف حداکثر ۵۰ کاراکتر");

        RuleFor(x => x.Type)
            .Must(t => t is "Percentage" or "Fixed")
            .WithMessage("نوع تخفیف باید Percentage یا Fixed باشد");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("مقدار تخفیف باید بیشتر از صفر باشد");

        When(x => x.Type == "Percentage", () =>
        {
            RuleFor(x => x.Value)
                .LessThanOrEqualTo(100).WithMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد");
        });

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate).WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).WithMessage("محدودیت استفاده باید بیشتر از صفر باشد");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("حداقل مبلغ سفارش نمی‌تواند منفی باشد");
    }
}
