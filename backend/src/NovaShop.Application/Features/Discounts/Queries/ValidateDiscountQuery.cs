using MediatR;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Discounts.Queries;

public record ValidateDiscountQuery(string Code) : IRequest<ValidateDiscountResult>;

public record ValidateDiscountResult(
    bool IsValid,
    string Code,
    string Type,
    decimal Value,
    decimal DiscountAmount,
    string? Message);

public class ValidateDiscountQueryHandler : IRequestHandler<ValidateDiscountQuery, ValidateDiscountResult>
{
    private readonly IDiscountRepository _discountRepository;

    public ValidateDiscountQueryHandler(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<ValidateDiscountResult> Handle(ValidateDiscountQuery request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByCodeIgnoringCaseAsync(request.Code.Trim());
        if (discount == null)
            throw new InvalidOperationException("کد تخفیف معتبر نیست");

        if (!discount.IsValid(DateTime.UtcNow))
            throw new InvalidOperationException("کد تخفیف منقضی شده یا غیرفعال است");

        return new ValidateDiscountResult(
            IsValid: true,
            Code: discount.Code,
            Type: discount.Type.ToString(),
            Value: discount.Value,
            DiscountAmount: discount.CalculateDiscount(100_000m),
            Message: null);
    }
}
