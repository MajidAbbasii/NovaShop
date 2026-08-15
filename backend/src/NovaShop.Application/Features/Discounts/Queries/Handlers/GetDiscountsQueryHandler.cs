using MediatR;
using NovaShop.Application.Features.Discounts.Dtos;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class GetDiscountsQueryHandler : IRequestHandler<Queries.GetDiscountsQuery, Domain.Common.PagedResult<Dtos.DiscountDto>>
{
    private readonly IDiscountRepository _repository;

    public GetDiscountsQueryHandler(IDiscountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Domain.Common.PagedResult<Dtos.DiscountDto>> Handle(Queries.GetDiscountsQuery request, CancellationToken cancellationToken)
    {
        var paged = await _repository.GetAllAsync(request.PageNumber, request.PageSize);

        var items = paged.Items.Select(d => new DiscountDto
        {
            Id = d.Id,
            Code = d.Code,
            Type = d.Type.ToString(),
            Value = d.Value,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            UsageLimit = d.UsageLimit,
            UsedCount = d.UsedCount,
            MinOrderAmount = d.MinOrderAmount,
            ApplicableProductIds = d.ApplicableProductIds,
            ApplicableCategoryIds = d.ApplicableCategoryIds,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();

        return new Domain.Common.PagedResult<Dtos.DiscountDto>(
            items, paged.TotalCount, paged.PageNumber, paged.PageSize, paged.TotalPages);
    }
}
