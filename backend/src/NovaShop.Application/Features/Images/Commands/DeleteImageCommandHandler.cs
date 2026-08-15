using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Infrastructure.Services;

namespace NovaShop.Application.Features.Images.Commands;

public class DeleteImageCommandHandler : IRequestHandler<DeleteImageCommand, bool>
{
    private readonly IImageStorage _imageStorage;
    private readonly ICacheService _cache;

    public DeleteImageCommandHandler(IImageStorage imageStorage, ICacheService cache)
    {
        _imageStorage = imageStorage;
        _cache = cache;
    }

    public async Task<bool> Handle(DeleteImageCommand request, CancellationToken cancellationToken)
    {
        var success = await _imageStorage.DeleteAsync(request.PublicId);
        
        if (success)
        {
            // Clear cache entries
            await _cache.RemoveByPrefixAsync("products");
            await _cache.RemoveByPrefixAsync("categories");
        }

        return success;
    }
}
