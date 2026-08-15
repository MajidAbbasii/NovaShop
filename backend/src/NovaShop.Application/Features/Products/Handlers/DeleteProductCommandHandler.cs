using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Products.Handlers;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public DeleteProductCommandHandler(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        if (product == null) return false;

        await _repository.DeleteAsync(request.Id);
        await _cache.RemoveByPrefixAsync(GetProductsQueryHandler.CachePrefix);
        return true;
    }
}
