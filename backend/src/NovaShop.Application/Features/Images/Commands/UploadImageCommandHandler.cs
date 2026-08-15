using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Infrastructure.Services;

namespace NovaShop.Application.Features.Images.Commands;

public class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, ImageUploadResult>
{
    private readonly IImageStorage _imageStorage;
    private readonly ICacheService _cache;

    public UploadImageCommandHandler(IImageStorage imageStorage, ICacheService cache)
    {
        _imageStorage = imageStorage;
        _cache = cache;
    }

    public async Task<ImageUploadResult> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        var result = await _imageStorage.UploadAsync(request.File, request.Folder, request.PublicId);
        
        // Clear cache entries related to products/images
        await _cache.RemoveByPrefixAsync("products");
        await _cache.RemoveByPrefixAsync("categories");

        return result;
    }
}
