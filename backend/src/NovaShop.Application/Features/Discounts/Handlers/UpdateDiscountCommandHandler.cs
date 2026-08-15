using MediatR;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class UpdateDiscountCommandHandler : IRequestHandler<Commands.UpdateDiscountCommand, bool>
{
    private readonly IDiscountRepository _repository;
    private readonly ILogger<UpdateDiscountCommandHandler> _logger;

    public UpdateDiscountCommandHandler(IDiscountRepository repository, ILogger<UpdateDiscountCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(Commands.UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await _repository.GetByIdAsync(request.Id);
        if (discount == null) return false;

        discount.Code = request.Code;
        discount.Type = request.Type == "Percentage" ? Domain.Entities.DiscountType.Percentage : Domain.Entities.DiscountType.Fixed;
        discount.Value = request.Value;
        discount.StartDate = request.StartDate;
        discount.EndDate = request.EndDate;
        discount.UsageLimit = request.UsageLimit;
        discount.MinOrderAmount = request.MinOrderAmount;
        discount.ApplicableProductIds = request.ApplicableProductIds;
        discount.ApplicableCategoryIds = request.ApplicableCategoryIds;
        discount.IsActive = request.IsActive;
        discount.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(discount);
        _logger.LogInformation("Discount {DiscountId} updated", request.Id);
        return true;
    }
}
