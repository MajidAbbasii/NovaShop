using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Dtos;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Application.Mappers;
using NovaShop.Common;
using NovaShop.Domain.Common;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Products.Handlers;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public const string CachePrefix = "products_";

    private readonly IProductRepository _repository;
    private readonly ProductMapper _mapper;
    private readonly ICacheService _cache;

    public GetProductsQueryHandler(IProductRepository repository, ProductMapper mapper, ICacheService cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("GetProducts");
        string cacheKey = $"{CachePrefix}{request.GetHashCode()}";

        if (await _cache.GetAsync<PagedResult<ProductDto>>(cacheKey) is { } cached)
            return cached;

        var pagedResult = await _repository.GetAllAsync(
            request.SearchTerm,
            request.MinPrice,
            request.MaxPrice,
            request.OnlyAvailable,
            request.PageNumber,
            request.PageSize,
            request.CategoryId
        );

        var result = new PagedResult<ProductDto>(
            _mapper.ToDtoList(pagedResult.Items),
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize,
            pagedResult.TotalPages
        );
        // ذخیره در Cache (۱۰ دقیقه)
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }
}
