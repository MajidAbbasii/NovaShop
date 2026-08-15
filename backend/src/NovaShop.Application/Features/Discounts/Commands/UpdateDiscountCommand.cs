using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Discounts.Commands;

public record UpdateDiscountCommand : IRequest<bool>
{
    public int Id { get; init; }
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

public class UpdateDiscountCommandValidator : AbstractValidator<UpdateDiscountCommand>
{
    public UpdateDiscountCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).Must(t => t is "Percentage" or "Fixed");
        RuleFor(x => x.Value).GreaterThan(0);
        When(x => x.Type == "Percentage", () =>
            RuleFor(x => x.Value).LessThanOrEqualTo(100));
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate);
        RuleFor(x => x.UsageLimit).GreaterThan(0);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0);
    }
}
