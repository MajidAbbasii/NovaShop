using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Products.Dtos;

namespace NovaShop.Application.Features.Products.Queries;

public record GetProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public string? SearchTerm { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? OnlyAvailable { get; init; }
    public int? CategoryId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
