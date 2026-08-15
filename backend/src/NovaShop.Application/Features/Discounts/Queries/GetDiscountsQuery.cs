using MediatR;
using NovaShop.Domain.Common;

namespace NovaShop.Application.Features.Discounts.Queries;

public record GetDiscountsQuery : IRequest<PagedResult<Discounts.Dtos.DiscountDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
