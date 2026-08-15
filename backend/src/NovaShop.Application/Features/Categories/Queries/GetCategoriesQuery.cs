using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Categories.Dtos;

namespace NovaShop.Application.Features.Categories.Queries;

public record GetCategoriesQuery : IRequest<PagedResult<CategoryDto>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record GetCategoryQuery(int Id) : IRequest<CategoryDto>;
