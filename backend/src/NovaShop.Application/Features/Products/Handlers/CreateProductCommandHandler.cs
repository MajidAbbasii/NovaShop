using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Products.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public CreateProductCommandHandler(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            ImageUrl = request.ImageUrl,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
        };

        foreach (var color in request.Colors)
        {
            product.Colors.Add(new ProductColor
            {
                Name = color.Name,
                HexCode = color.HexCode ?? string.Empty,
                Stock = color.Stock,
                Price = color.Price,
                IsActive = color.IsActive,
                CreatedAt = DateTime.UtcNow,
            });
        }

        // Colors are new entities — use navigation links so EF resolves FK ids
        // within the same SaveChanges (no batched FK violation).
        var newColors = product.Colors.ToList();
        foreach (var img in request.Images.OrderBy(i => i.DisplayOrder))
        {
            var entity = new ProductImage
            {
                Url = img.Url,
                AltText = img.AltText ?? string.Empty,
                DisplayOrder = img.DisplayOrder,
                IsPrimary = img.IsPrimary,
                CreatedAt = DateTime.UtcNow,
            };

            if (img.ProductColorId.HasValue)
            {
                var idx = img.ProductColorId.Value;
                if (idx < 0 || idx >= newColors.Count)
                    throw new InvalidOperationException("تصویر به رنگ نامعتبر متصل شده است");
                entity.ProductColor = newColors[idx];
            }

            product.Images.Add(entity);
        }

        var id = await _repository.AddAsync(product);

        await _cache.RemoveByPrefixAsync(GetProductsQueryHandler.CachePrefix);
        return id;
    }
}
