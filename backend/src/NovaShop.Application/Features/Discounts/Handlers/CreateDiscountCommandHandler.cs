using MediatR;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class CreateDiscountCommandHandler : IRequestHandler<Commands.CreateDiscountCommand, int>
{
    private readonly IDiscountRepository _repository;
    private readonly ILogger<CreateDiscountCommandHandler> _logger;

    public CreateDiscountCommandHandler(IDiscountRepository repository, ILogger<CreateDiscountCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> Handle(Commands.CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Code);
        if (existing != null)
            throw new InvalidOperationException($"کد تخفیف '{request.Code}' قبلاً ثبت شده است");

        var discount = new Domain.Entities.Discount
        {
            Code = request.Code,
            Type = request.Type == "Percentage" ? Domain.Entities.DiscountType.Percentage : Domain.Entities.DiscountType.Fixed,
            Value = request.Value,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            MinOrderAmount = request.MinOrderAmount,
            ApplicableProductIds = request.ApplicableProductIds,
            ApplicableCategoryIds = request.ApplicableCategoryIds,
            IsActive = request.IsActive
        };

        var id = await _repository.AddAsync(discount);
        _logger.LogInformation("Discount {DiscountId} created with code {Code}", id, request.Code);
        return id;
    }
}
