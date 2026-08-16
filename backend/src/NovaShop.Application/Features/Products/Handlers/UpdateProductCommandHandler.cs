using MediatR;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Products.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly NovaShopDbContext _context;

    public UpdateProductCommandHandler(IProductRepository repository, ICacheService cache, NovaShopDbContext context)
    {
        _repository = repository;
        _cache = cache;
        _context = context;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        if (product == null) return false;

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.OriginalPrice != null) product.OriginalPrice = request.OriginalPrice;
        if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
        if (request.Stock.HasValue)
        {
            // NEVER clobber active reservations. Admin may adjust stock, but the
            // total physical inventory must stay >= ReservedQuantity so outstanding
            // orders are not destroyed. The requested value is treated as the new
            // AVAILABLE stock; ReservedQuantity is preserved on top of it.
            var reserved = product.ReservedQuantity;
            if (request.Stock.Value < reserved)
                throw new InvalidOperationException(
                    $"نمی‌توان موجودی را کمتر از مقدار رزرو شده ({reserved}) تنظیم کرد. " +
                    $"محصول {product.Name} دارای {reserved} عدد رزرو فعال است.");
            product.Stock = request.Stock.Value;
        }
        if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;

        if (request.Colors != null)
        {
            // Remove old images+colors; re-add from request.
            foreach (var oldImg in product.Images.ToList())
                _context.ProductImages.Remove(oldImg);
            product.Images.Clear();
            foreach (var oldColor in product.Colors.ToList())
                _context.ProductColors.Remove(oldColor);
            product.Colors.Clear();
            await _context.SaveChangesAsync(cancellationToken); // flush removals so FK is clean

            var newColors = new List<ProductColor>();
            foreach (var color in request.Colors)
            {
                var entity = new ProductColor
                {
                    Name = color.Name,
                    HexCode = color.HexCode ?? string.Empty,
                    Stock = color.Stock,
                    Price = color.Price,
                    IsActive = color.IsActive,
                    CreatedAt = DateTime.UtcNow,
                };
                product.Colors.Add(entity);
                newColors.Add(entity);
            }

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
                    entity.ProductColor = newColors[idx]; // link via navigation – EF resolves FK
                }
                product.Images.Add(entity);
            }

            var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
            if (primary != null)
                product.ImageUrl = primary.Url;
        }

        await _repository.UpdateAsync(product);
        await _cache.RemoveByPrefixAsync(GetProductsQueryHandler.CachePrefix);
        return true;
    }
}