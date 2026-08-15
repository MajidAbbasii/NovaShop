using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Products.Dtos;

namespace NovaShop.Application.Features.Products.Queries;

public record SearchProductsQuery : IRequest<PagedResult<ProductSearchDto>>
{
    public string Query { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    /// <summary>
    /// Sort mode: "relevance" (default), "price_asc", "price_desc", "name".
    /// </summary>
    public string SortBy { get; init; } = "relevance";
}
